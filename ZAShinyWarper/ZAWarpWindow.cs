using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKHeX.Drawing.PokeSprite;
using SysBot.Base;
using ZAShinyWarper.helpers;
using ZAShinyWarper.Hunting;

namespace ZAShinyWarper
{
    public partial class ZAWarpWindow : Form
    {
        private string WarperTitle = "Z-A Shiny Warper";
        private List<Vector3> positions = [];
        private const string Config = "config.json";
        private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

        private readonly CancellationTokenSource _globalCts = new();
        private CancellationToken GlobalToken => _globalCts.Token;
        private CancellationTokenSource? _monitoringCts = null;

        private ConnectionWrapper ConnectionWrapper = default!;
        private static readonly Lock ConnectionLock = new();
        private readonly SwitchConnectionConfig ConnectionConfig;
        private ProgramConfig programConfig = new();
        public ComboBox[] CBIVs = default!;

        private readonly ShinyHunter<PA9> shinyHunter = new();
        private readonly List<PictureBox> StashList;
        private static readonly HttpClient httpClient = new();
        private readonly WarpProgressForm warpProgress = new();
        private ContextMenuStrip? shinyContextMenu;

        private readonly List<string> filterSpecies = [];
        private readonly Dictionary<string, bool> speciesCheckStates = [];

        // Bot
        private bool matchingShinyFound = false;
        private bool monitoring = false;
        private bool warping = false;
        private int currentWarps = 0;
        private DateTime startTime;

        public ZAWarpWindow()
        {
            InitializeComponent();
            SetupListBox();
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            warpProgress.CancelRequested += (s, e) => warping = false;

            FormClosing += OnFormClosing;

            SpriteName.AllowShinySprite = true;
            StashList = [StashedShiny1, StashedShiny2, StashedShiny3, StashedShiny4, StashedShiny5, StashedShiny6, StashedShiny7, StashedShiny8, StashedShiny9, StashedShiny10];

            LoadConfig();
            ConnectionConfig = new()
            {
                IP = programConfig.IPAddress,
                Port = programConfig.Protocol is SwitchProtocol.WiFi ? 6000 : programConfig.UsbPort,
                Protocol = programConfig.Protocol,
            };

            warpTimer.Interval = 100;
            warpTimer.Tick += WarpTimerTick;
        }

        private void WarpTimerTick(object? sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            Text = $"{WarperTitle} - Warping For: {elapsed:hh\\:mm\\:ss}";
            warperIcon.Text = $"Warping For: {elapsed:hh\\:mm\\:ss}";
            warperIcon.Visible = true;
        }

        private void ResetTimer()
        {
            Invoke(() =>
            {
                warpTimer.Stop();
                Text = $"{WarperTitle}";
                warperIcon.Text = Text;
                warperIcon.Visible = false;
            });
        }

        private void OnClickTrayIcon(object sender, EventArgs e)
        {
            Activate();
            BringToFront();
            Focus();
            Show();
            WindowState = FormWindowState.Normal;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cancel all operations
                _globalCts?.Cancel();
                _monitoringCts?.Cancel();

                // Dispose managed resources
                httpClient?.Dispose();
                warpProgress?.Dispose();
                shinyContextMenu?.Dispose();
                _monitoringCts?.Dispose();
                _globalCts?.Dispose();
                shinyHunter?.Dispose();

                // Designer-managed components
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private async void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel ongoing operations
            _globalCts?.Cancel();
            _monitoringCts?.Cancel();

            // Save Configurations
            SaveConfig();

            // Clean up bot connection
            await CleanUpBotAsync();
        }

        private void LoadDefaults(object sender, EventArgs e)
        {
            // Load enums into comboboxes
            // Connection Protocol
            foreach (var item in Enum.GetValues<SwitchProtocol>())
                cBProtocol.Items.Add(item);
            // WhenShinyFound
            foreach (var item in Enum.GetValues<ShinyFoundAction>())
                cBWhenShinyFound.Items.Add(item);

            // Forced Weather
            foreach (var item in Enum.GetValues<Weather>())
                cBForcedWeather.Items.Add(item);

            // Forced Time of Day
            foreach (var item in Enum.GetValues<TimeOfDay>())
                cBForcedTimeOfDay.Items.Add(item);

            // Species
            cBSpecies.Items.Add("Any");
            filterSpecies.Add("Any");

            foreach (var item in ZAZukan.PokedexNumbersZA)
            {
                string speciesEntry = $"{item.Value:D3} - {item.Key}";
                cBSpecies.Items.Add(speciesEntry);
                filterSpecies.Add(speciesEntry);
                speciesCheckStates[speciesEntry] = false;
            }
            speciesCheckStates["Any"] = true;
            cBSpecies.SetItemChecked(0, true);

            // IVs
            CBIVs =
            [
                cBIVHP,
                cBIVAtk,
                cBIVDef,
                cBIVSpA,
                cBIVSpD,
                cBIVSpe
            ];

            foreach (var cb in CBIVs)
            {
                foreach (var item in Enum.GetValues<IVType>())
                    cb.Items.Add(item);
            }

            // Try to load existing config
            bool configExists = File.Exists(Config) || File.Exists("positions.txt") || File.Exists("filter_config.txt");

            if (configExists)
            {
                LoadConfig();
                UpdateUI();
            }
            else
            {
                // Set defaults for new users
                cBWhenShinyFound.SelectedIndex = 0;
                cBForcedWeather.SelectedIndex = 8;
                cBForcedTimeOfDay.SelectedIndex = 4;
                cBSpecies.SetItemChecked(0, true);

                foreach (var cb in CBIVs)
                    cb.SelectedIndex = 0;

                nUDDistance.Value = 3;

                UpdateUI();
            }
        }

        private ShinyFilter<PA9> GetFilter()
        {
            var filter = new ShinyFilter<PA9>();
            // Species - collect all checked species
            var checkedSpecies = new List<ushort>();
            for (int i = 0; i < cBSpecies.Items.Count; i++)
            {
                if (cBSpecies.GetItemChecked(i) && i > 0) // Skip "Any" at index 0
                {
                    var sel = cBSpecies.Items[i].ToString()!;
                    var spl = sel.Split(" - ");
                    checkedSpecies.Add(ushort.Parse(spl[0]));
                }
            }

            // If "Any" is checked or no species selected, leave filter.Species as null
            // Otherwise, set the list of species
            if (checkedSpecies.Count > 0)
                filter.SpeciesList = checkedSpecies;

            // IVs
            for (int i = 0; i < 6; i++)
            {
                filter.IVs[i] = (IVType)CBIVs[i].SelectedItem!;
            }

            // Size
            filter.SizeMinimum = (byte)nUDScaleMin.Value;
            filter.SizeMaximum = (byte)nUDScaleMax.Value;
            // Alpha
            filter.IsAlpha = cBIsAlpha.Checked;
            return filter;
        }

        private void OnClickConnect(object sender, EventArgs e)
        {
            lock (ConnectionLock)
            {
                if (ConnectionWrapper != null && ConnectionWrapper.Connected)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (warping)
                            {
                                warping = false;
                                ResetTimer();
                                SetFiltersEnableState(true);
                                Invoke(() => btnWarp.Text = "Start Warping");
                            }

                            Invoke(() =>
                            {
                                tB_IP.Enabled = true;
                                cBProtocol.Enabled = true;
                                tB_Port.Enabled = cBProtocol.SelectedIndex == 1;
                            });
                            SetUIEnableState(false);
                            await CleanUpBotAsync();
                            await ConnectionWrapper.Disconnect(GlobalToken).ConfigureAwait(false);
                            ResetSprites();
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected during shutdown
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error during disconnect: {ex.Message}");
                        }
                    }, GlobalToken);
                }
                else
                {
                    try
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                Invoke(() =>
                                {
                                    ConnectionConfig.IP = tB_IP.Text;
                                    ConnectionConfig.Port = int.Parse(tB_Port.Text);
                                    ConnectionConfig.Protocol = (SwitchProtocol)cBProtocol.SelectedItem!;

                                    btnConnect.Enabled = false;
                                    tB_IP.Enabled = false;
                                    tB_Port.Enabled = false;
                                    cBProtocol.Enabled = false;
                                });
                                ConnectionWrapper = new(ConnectionConfig);

                                await ConnectionWrapper.Connect(GlobalToken);
                                Invoke(() => btnConnect.Enabled = true);
                                shinyHunter.Initialize(ConnectionWrapper);
                                await shinyHunter.LoadStashedShinies(GlobalToken);
                                DisplayStashedShinies();
                                DisplayStashedMessageBox(true);
                                SetUIEnableState(true);
                                await CleanUpBotAsync();
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected during shutdown
                            }
                            catch (Exception ex)
                            {
                                BeginInvoke(() => MessageBox.Show(ex.Message));
                                Invoke(() =>
                                {
                                    tB_IP.Enabled = true;
                                    cBProtocol.Enabled = true;
                                    tB_Port.Enabled = cBProtocol.SelectedIndex == 1;
                                    btnConnect.Enabled = true;
                                });
                            }
                        }, GlobalToken);
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke(() => MessageBox.Show(ex.Message));
                    }
                }
            }
        }

        private void DisplayStashedMessageBox(bool wifi)
        {
            Invoke(() =>
            {
                var form = new Form
                {
                    Text = "Connected",
                    Width = 250,
                    Height = 175,
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ControlBox = false
                };

                var headerLabel = new Label
                {
                    Text = $"SysBot Connected\r\n({(wifi ? "Network" : "USB")})",
                    Font = new Font("Segoe UI", 12),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Top,
                    Height = 65,
                    Padding = new Padding(10)
                };
                var count = shinyHunter.StashedShinies.Count;
                var countLabel = new Label
                {
                    Text = $"{(count == 0 ? "No Shinies found in your stash." : $"{count} Shin{(count == 1 ? "y" : "ies")} found in your stash!")}",
                    Font = new Font("Segoe UI", 10),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };

                var okButton = new Button
                {
                    Text = "OK",
                    Width = 80,
                    Height = 30,
                    Dock = DockStyle.Bottom,
                    Margin = new Padding(10)
                };
                okButton.Click += (s, e) => form.Close();

                form.Controls.Add(countLabel);
                form.Controls.Add(headerLabel);
                form.Controls.Add(okButton);
                form.ShowDialog();
            });
        }

        private async void OnClickScreenOn(object sender, EventArgs e)
        {
            await ConnectionWrapper.ToggleScreen(true, GlobalToken).ConfigureAwait(false);
        }

        private async void OnClickScreenOff(object sender, EventArgs e)
        {
            await ConnectionWrapper.ToggleScreen(false, GlobalToken).ConfigureAwait(false);
        }

        private async void OnClickForwards(object sender, EventArgs e)
        {
            int distance = 0;
            Invoke(() => distance = (int)nUDDistance.Value);
            await ConnectionWrapper.MovePlayer(0, 1, 0, distance, GlobalToken);
        }

        private async void OnClickBackwards(object sender, EventArgs e)
        {
            int distance = 0;
            Invoke(() => distance = (int)nUDDistance.Value);
            await ConnectionWrapper.MovePlayer(0, -1, 0, distance, GlobalToken);
        }

        private async void OnClickLeft(object sender, EventArgs e)
        {
            int distance = 0;
            Invoke(() => distance = (int)nUDDistance.Value);
            await ConnectionWrapper.MovePlayer(1, 0, 0, distance, GlobalToken);
        }

        private async void OnClickRight(object sender, EventArgs e)
        {
            int distance = 0;
            Invoke(() => distance = (int)nUDDistance.Value);
            await ConnectionWrapper.MovePlayer(-1, 0, 0, distance, GlobalToken);
        }

        private void OnClickMoveItemUp(object sender, EventArgs e)
        {
            if (lBCoords.SelectedIndex > 0)
            {
                int index = lBCoords.SelectedIndex;
                (positions[index - 1], positions[index]) = (positions[index], positions[index - 1]);
                SaveAllAndUpdateUI();
                lBCoords.SelectedIndex = index - 1;
            }
        }

        private void OnClickMoveItemDown(object sender, EventArgs e)
        {
            if (lBCoords.SelectedIndex >= 0 &&
                lBCoords.SelectedIndex < lBCoords.Items.Count - 1)
            {
                int index = lBCoords.SelectedIndex;
                (positions[index + 1], positions[index]) = (positions[index], positions[index + 1]);
                SaveAllAndUpdateUI();
                lBCoords.SelectedIndex = index + 1;
            }
        }

        private async void OnClickUp(object sender, EventArgs e)
        {
            int distance = 0;
            Invoke(() => distance = (int)nUDDistance.Value);
            await ConnectionWrapper.MovePlayer(0, 0, 1, distance, GlobalToken);
        }

        private async void OnClickSave(object sender, EventArgs e)
        {
            var pos = await ConnectionWrapper.GetPlayerPositionAsync(GlobalToken).ConfigureAwait(false);
            if (pos.X == 0 || pos.Z == 0)
                return;

            positions.Add(pos);
            SaveAllAndUpdateUI();

            Invoke(() => lBCoords.SelectedIndex = lBCoords.Items.Count - 1);
        }

        private async void OnClickRestore(object sender, EventArgs e)
        {
            Vector3? toSend = null;
            int selectedIndex = -1;

            Invoke(() =>
            {
                selectedIndex = lBCoords.SelectedIndex;
                if (selectedIndex > -1 && lBCoords.SelectedItem != null)
                    toSend = (Vector3)lBCoords.SelectedItem;
            });

            if (!toSend.HasValue)
                return;

            await TeleportPlayer(toSend.Value);
        }

        private async Task TeleportPlayer(Vector3 toSend, bool spawner = false)
        {
            warping = true;
            SetWarpingEnableState(false);
            await ConnectionWrapper.SetPlayerPosition(toSend.X, toSend.Y, toSend.Z, GlobalToken);
            Invoke(() =>
            {
                CenterFormOnParent(warpProgress);
                warpProgress.Show();
            });
            await Task.Delay(250);

            for (int i = 0; i < 15; ++i)
            {
                if (!warping)
                {
                    Invoke(() =>
                    {
                        warpProgress.Hide();
                        warpProgress.SetText("Warping...");
                    });
                    SetWarpingEnableState(true);
                    return;
                }

                if (spawner)
                {
                    await ConnectionWrapper.MarkSpawn(GlobalToken).ConfigureAwait(false);
                    spawner = false;
                }

                int currentAttempt = i + 1;
                var pos = await ConnectionWrapper.GetPlayerPositionAsync(GlobalToken).ConfigureAwait(false);

                if (pos.X == 0 || pos.Z == 0)
                {
                    Invoke(() =>
                    {
                        warpProgress.Hide();
                        warpProgress.SetText("Warping...");
                    });
                    warping = false;
                    SetWarpingEnableState(true);
                    Invoke(() => MessageBox.Show("Warp failed. Please check Switch Connection"));
                    return;
                }

                if (Math.Abs(pos.Y - toSend.Y) <= 0.2f)
                    break;

                if (i == 0)
                    await Task.Delay(4000);
                else
                    Invoke(() => warpProgress.SetText($"Reattempting to Warp ({currentAttempt}/15) Please wait."));

                await ConnectionWrapper.SetPlayerPosition(toSend.X, toSend.Y, toSend.Z, GlobalToken);
                await Task.Delay(1100).ConfigureAwait(false);

                var position = await ConnectionWrapper.GetPlayerPositionAsync(GlobalToken).ConfigureAwait(false);
                if (i == 14 && position.Y != toSend.Y)
                {
                    Invoke(() => warpProgress.SetText("Warp failure. Please retry"));
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }

            Invoke(() =>
            {
                warpProgress.Hide();
                warpProgress.SetText("Warping...");
            });
            warping = false;
            SetWarpingEnableState(true);
        }

        private void OnClickExport(object sender, EventArgs e)
        {
            if (lBCoords.Items.Count == 0)
            {
                MessageBox.Show("No items to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog saveDialog = new();
            saveDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            saveDialog.DefaultExt = "txt";
            saveDialog.FileName = "coordinates.txt";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter writer = new(saveDialog.FileName))
                    {
                        foreach (var item in lBCoords.Items)
                        {
                            writer.WriteLine(item.ToString());
                        }
                    }
                    MessageBox.Show($"Successfully exported {lBCoords.Items.Count} items.",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting items: {ex.Message}",
                        "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnClickImport(object sender, EventArgs e)
        {
            using OpenFileDialog openDialog = new();
            openDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            openDialog.DefaultExt = "txt";
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openDialog.FileName);

                    bool shouldClear = true; // Likely to always clear

                    // Only ask if there are existing items
                    if (positions.Count > 0)
                    {
                        DialogResult result = MessageBox.Show(this,
                            "Do you want to overwrite the current coordinates?\n\n" +
                            "Yes = Overwrite existing\n" +
                            "No = Add to existing\n",
                            "Import Options",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Cancel)
                        {
                            return; // User cancelled
                        }

                        shouldClear = (result == DialogResult.Yes);
                    }

                    // If overwrite, clear existing data
                    if (shouldClear)
                    {
                        lBCoords.Items.Clear();
                        positions.Clear();
                    }

                    // Import the data
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            Vector3 vector = Vector3.FromString(line);
                            lBCoords.Items.Add(vector);
                            positions.Add(vector);
                        }
                    }

                    string action = shouldClear ? "imported" : "added";
                    MessageBox.Show($"Successfully {action} {lines.Length} items.",
                        "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing items: {ex.Message}",
                        "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            SaveAllAndUpdateUI();
        }

        private void CenterFormOnParent(Form childForm)
        {
            Invoke(() =>
            {
                childForm.StartPosition = FormStartPosition.Manual;
                int x = Location.X + (Width - childForm.Width) / 2;
                int y = Location.Y + (Height - childForm.Height) / 2;
                childForm.Location = new Point(x, y);
            });
        }

        private void OnClickDelete(object sender, EventArgs e)
        {
            if (lBCoords.SelectedIndex > -1)
            {
                var removedIndex = lBCoords.SelectedIndex;
                positions.RemoveAt(lBCoords.SelectedIndex);
                SaveAllAndUpdateUI();

                if (removedIndex >= lBCoords.Items.Count)
                    lBCoords.SelectedIndex = lBCoords.Items.Count - 1;
                else
                    lBCoords.SelectedIndex = removedIndex;
            }
        }

        private async void OnClickRefresh(object sender, EventArgs e)
        {
            if (sender == btnRefreshTime)
            {
                if (ConnectionWrapper.Connected)
                {
                    try
                    {
                        TimeOfDay timeOfDay = TimeOfDay.Morning;
                        Invoke(() => timeOfDay = (TimeOfDay)cBForcedTimeOfDay.SelectedItem!);
                        await shinyHunter.SetTime(timeOfDay, GlobalToken, false);
                    }
                    catch
                    {
                        BeginInvoke(() => MessageBox.Show("Error Setting the time. Please check your connection to the Switch"));
                    }
                }
                else
                {
                    BeginInvoke(() => MessageBox.Show("Not connected to SysBot."));
                }
            }
            else if (sender == btnRefreshWeather)
            {
                if (ConnectionWrapper.Connected)
                {
                    try
                    {
                        Weather weather = Weather.None;
                        Invoke(() => weather = (Weather)cBForcedWeather.SelectedItem!);
                        await shinyHunter.SetWeather(weather, GlobalToken, false);
                    }
                    catch
                    {
                        BeginInvoke(() => MessageBox.Show("Error Setting the weather. Please check your connection to the Switch"));
                    }
                }
                else
                {
                    BeginInvoke(() => MessageBox.Show("Not connected to SysBot."));
                }
            }
        }

        private void OnClickReset(object sender, EventArgs e)
        {
            // Make sure search box is cleared
            tbSpeciesSearch.Text = string.Empty;

            // Uncheck all items in the dictionary
            foreach (var key in speciesCheckStates.Keys.ToList())
            {
                speciesCheckStates[key] = false;
            }

            // Check "Any" in the dictionary
            speciesCheckStates["Any"] = true;

            // Update the visible CheckedListBox
            for (int i = 0; i < cBSpecies.Items.Count; i++)
            {
                cBSpecies.SetItemChecked(i, false);
            }
            // Check "Any" (should be index 0 after search is cleared)
            if (cBSpecies.Items.Count > 0 && cBSpecies.Items[0].ToString() == "Any")
            {
                cBSpecies.SetItemChecked(0, true);
            }
            SaveConfig();
        }

        private void OnClickResetFilters(object sender, EventArgs e)
        {
            // Reset all filters to default
            for (int i = 0; i < cBSpecies.Items.Count; i++)
            {
                cBSpecies.SetItemChecked(i, false);
            }
            cBSpecies.SetItemChecked(0, true);


            foreach (var cb in CBIVs)
            {
                cb.SelectedIndex = 0;
            }

            nUDScaleMin.Value = 0;
            nUDScaleMax.Value = 255;
            SaveConfig();
        }

        private void SaveAllAndUpdateUI()
        {
            SaveConfig();
            UpdateUI();
        }

        private void UpdateUI()
        {
            Invoke(() =>
                {
                    lBCoords.SelectedIndex = -1;
                    lBCoords.Items.Clear();
                    foreach (var pos in positions)
                        lBCoords.Items.Add(pos);
                });
        }

        private void OnAlphaCheckedChanged(object? sender, EventArgs e)
        {
            bool isChecked = cBIsAlpha.Checked;

            if (isChecked)
            {
                nUDScaleMin.Value = 255;
                nUDScaleMin.Enabled = false;

                nUDScaleMax.Value = 255;
                nUDScaleMax.Enabled = false;

            }
            else
            {
                nUDScaleMin.Enabled = true;
                nUDScaleMin.Value = 0;

                nUDScaleMax.Enabled = true;
                nUDScaleMax.Value = 255;
            }
        }

        private void OnProtocolChanged(object? sender, EventArgs e)
        {
            bool isWiFi = (SwitchProtocol)cBProtocol.SelectedIndex == SwitchProtocol.WiFi;

            if (isWiFi)
            {
                tB_Port.Text = "6000";
                tB_Port.Enabled = false;
                programConfig.Protocol = SwitchProtocol.WiFi;
            }
            else
            {
                tB_Port.Text = programConfig.UsbPort.ToString();
                tB_Port.Enabled = true;
                programConfig.Protocol = SwitchProtocol.USB;
            }
        }

        private void SaveConfig()
        {
            try
            {
                programConfig.IPAddress = tB_IP.Text;
                if (programConfig.Protocol is SwitchProtocol.USB)
                    programConfig.UsbPort = int.Parse(tB_Port.Text);
                programConfig.Protocol = (SwitchProtocol)cBProtocol.SelectedItem!;
                programConfig.Positions = positions;
                programConfig.WarpDistance = nUDDistance.Value;
                programConfig.SpawnCheckTime = nUDCheckTime.Value;
                programConfig.CamMove = nUDCamMove.Value;
                programConfig.SaveFreq = nUDSaveFreq.Value;
                programConfig.WhenShinyFound = cBWhenShinyFound.SelectedIndex;
                programConfig.ForcedWeather = cBForcedWeather.SelectedIndex;
                programConfig.ForcedTimeOfDay = cBForcedTimeOfDay.SelectedIndex;
                programConfig.IVHP = cBIVHP.SelectedIndex;
                programConfig.IVAtk = cBIVAtk.SelectedIndex;
                programConfig.IVDef = cBIVDef.SelectedIndex;
                programConfig.IVSpA = cBIVSpA.SelectedIndex;
                programConfig.IVSpD = cBIVSpD.SelectedIndex;
                programConfig.IVSpe = cBIVSpe.SelectedIndex;
                programConfig.IsAlpha = cBIsAlpha.Checked;
                programConfig.ScaleMin = nUDScaleMin.Value;
                programConfig.ScaleMax = nUDScaleMax.Value;

                programConfig.SpeciesIndices.Clear();
                for (int i = 0; i < filterSpecies.Count; i++)
                {
                    string item = filterSpecies[i];
                    if (speciesCheckStates.TryGetValue(item, out bool value) && value)
                        programConfig.SpeciesIndices.Add(i);
                }

                var json = JsonSerializer.Serialize(programConfig, jsonOptions);
                File.WriteAllText(Config, json);
            }
            catch
            {
                // Silently fail if we can't save config
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(Config))
                {
                    if (File.Exists("positions.txt") || File.Exists("filter_config.txt"))
                        LegacyImport();
                    else
                        return;
                }

                var json = File.ReadAllText(Config);
                var config = JsonSerializer.Deserialize<ProgramConfig>(json);

                if (config == null)
                    return;

                programConfig = config;

                // Clear all states first
                foreach (var key in speciesCheckStates.Keys.ToList())
                {
                    speciesCheckStates[key] = false;
                }

                // Load saved indices into dictionary
                foreach (var idx in programConfig.SpeciesIndices)
                {
                    if (idx < filterSpecies.Count)
                    {
                        speciesCheckStates[filterSpecies[idx]] = true;
                    }
                }

                // Apply to visible items
                for (int i = 0; i < cBSpecies.Items.Count; i++)
                {
                    string item = cBSpecies.Items[i].ToString()!;
                    cBSpecies.SetItemChecked(i, speciesCheckStates.ContainsKey(item) && speciesCheckStates[item]);
                }

                tB_IP.Text = programConfig.IPAddress;
                tB_Port.Text = programConfig.Protocol is SwitchProtocol.WiFi ? "6000" : programConfig.UsbPort.ToString();
                cBProtocol.SelectedItem = programConfig.Protocol;
                positions = programConfig.Positions;
                nUDDistance.Value = programConfig.WarpDistance;
                cBWhenShinyFound.SelectedIndex = programConfig.WhenShinyFound;
                cBForcedWeather.SelectedIndex = programConfig.ForcedWeather;
                cBForcedTimeOfDay.SelectedIndex = programConfig.ForcedTimeOfDay;
                nUDCheckTime.Value = programConfig.SpawnCheckTime;
                nUDCamMove.Value = programConfig.CamMove;
                nUDSaveFreq.Value = programConfig.SaveFreq;
                cBIVHP.SelectedIndex = programConfig.IVHP;
                cBIVAtk.SelectedIndex = programConfig.IVAtk;
                cBIVDef.SelectedIndex = programConfig.IVDef;
                cBIVSpA.SelectedIndex = programConfig.IVSpA;
                cBIVSpD.SelectedIndex = programConfig.IVSpD;
                cBIVSpe.SelectedIndex = programConfig.IVSpe;
                nUDScaleMin.Value = programConfig.ScaleMin;
                nUDScaleMax.Value = programConfig.ScaleMax;
                cBIsAlpha.Checked = programConfig.IsAlpha;
            }
            catch
            {
                // If loading fails, just use defaults
            }
        }

        private void SetUIEnableState(bool enabled)
        {
            Invoke(() =>
            {
                gBControls.Enabled = enabled;
                gBShinyHunt.Enabled = enabled;
                gBStashedShiny.Enabled = enabled;
                btnScreenOn.Enabled = enabled;
                btnScreenOff.Enabled = enabled;
                btnWebhookSettings.Enabled = enabled;
                btnWarp.Enabled = enabled;
                btnResetFilters.Enabled = enabled;
                btnExportSets.Enabled = enabled;
                btnMonitoring.Enabled = enabled;
                btnConnect.Text = enabled ? "Disconnect" : "Connect";
            });
        }

        private void SetFiltersEnableState(bool enabled)
        {

            Invoke(() =>
            {
                cBSpecies.Enabled = enabled;
                foreach (var cb in CBIVs)
                    cb.Enabled = enabled;
                cBWhenShinyFound.Enabled = enabled;
                cBForcedWeather.Enabled = enabled;
                cBForcedTimeOfDay.Enabled = enabled;
                btnRefreshWeather.Enabled = enabled;
                btnRefreshTime.Enabled = enabled;
                nUDCheckTime.Enabled = enabled;
                nUDCamMove.Enabled = enabled;
                nUDSaveFreq.Enabled = enabled;
                gBControls.Enabled = enabled;
                btnResetSpecies.Enabled = enabled;
                cBIsAlpha.Enabled = enabled;

                bool isAlphaChecked = false;
                isAlphaChecked = cBIsAlpha.Checked;

                if (isAlphaChecked)
                {
                    nUDScaleMin.Enabled = false;
                    nUDScaleMax.Enabled = false;
                }
                else
                {
                    nUDScaleMin.Enabled = enabled;
                    nUDScaleMax.Enabled = enabled;
                }
                btnResetFilters.Enabled = enabled;
                btnMonitoring.Enabled = enabled;
            });
        }

        private void SetWarpingEnableState(bool enabled)
        {

            Invoke(() =>
            {
                btnRestore.Enabled = enabled;
                btnSave.Enabled = enabled;
                btnDelete.Enabled = enabled;
                btnWarp.Enabled = enabled;
            });
        }

        private async Task CleanUpBotAsync()
        {
            try
            {
                shinyHunter.UnlockTime();
                shinyHunter.UnlockWeather();
                if (ConnectionWrapper != null && ConnectionWrapper.Connected)
                {
                    await ConnectionWrapper.OnClose(GlobalToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        private async void OnClickWarp(object sender, EventArgs e)
        {
            if (warping)
            {
                ResetTimer();
                warping = false;
                SetFiltersEnableState(true);
                Invoke(() => btnWarp.Text = "Start Warping");
                await CleanUpBotAsync();
                return;
            }

            if (positions.Count < 2)
            {
                MessageBox.Show("A minimum of 2 warp positions is required!");
                return;
            }

            startTime = DateTime.Now;
            warpTimer.Start();
            var filter = GetFilter();
            int warpInterval = 0;
            int camSpeed = 0;
            ShinyFoundAction action = ShinyFoundAction.StopOnFound;
            int saveFrequency = 0;

            Invoke(() =>
            {
                warpInterval = (int)nUDCheckTime.Value;
                camSpeed = (int)nUDCamMove.Value;
                action = (ShinyFoundAction)cBWhenShinyFound.SelectedItem!;
                saveFrequency = (int)nUDSaveFreq.Value;
            });

            currentWarps = 0;

            warping = true;
            matchingShinyFound = false;
            SetFiltersEnableState(false);
            Invoke(() => btnWarp.Text = "Warping. Click to end.");

            // Set Time and Weather
            TimeOfDay timeOfDay = TimeOfDay.Morning;
            Weather weather = Weather.None;

            Invoke(() =>
            {
                timeOfDay = (TimeOfDay)cBForcedTimeOfDay.SelectedItem!;
                weather = (Weather)cBForcedWeather.SelectedItem!;
            });


            // Rotate camera for spawns
            if (camSpeed != 0)
                await ConnectionWrapper.SetRotation(camSpeed, GlobalToken);

            // Refresh stashed shinies
            await shinyHunter.LoadStashedShinies(GlobalToken);
            DisplayStashedShinies();
            await shinyHunter.SetTime(timeOfDay, GlobalToken);
            await shinyHunter.SetWeather(weather, GlobalToken);

            while (warping)
            {
                currentWarps++;
                if (currentWarps % saveFrequency == 0)
                    await ConnectionWrapper.SaveGame(GlobalToken).ConfigureAwait(false);

                // Check shinies first as a new one may have spawned before we move
                var newFound = await shinyHunter.LoadStashedShinies(GlobalToken);
                var cacheIsFull = shinyHunter.StashedShinies.Count == 10;

                if (newFound)
                {
                    DisplayStashedShinies();
                    var newShinies = shinyHunter.DifferentShinies;
                    foreach (var pk in newShinies)
                    {
                        var matchesFilter = filter.MatchesFilter(pk.PKM);

                        if (matchesFilter)
                        {
                            matchingShinyFound = true;
                        }

                        var shouldStop = false;
                        var shouldSendEmbed = true;
                        var stopMessage = string.Empty;

                        if (matchesFilter && action == ShinyFoundAction.StopOnFound) // Found what we wanted. Stop warping, go catch it
                        {
                            shouldStop = true;
                            stopMessage = $"A Shiny {(pk.PKM.IsAlpha ? "Alpha " : "")}matching the filter has been found after {currentWarps} attempts!\r\nStopping warping.\r\n\r\n{pk}\r\n";
                        }
                        else if (matchesFilter && action == ShinyFoundAction.ClearAndContinue) // Found what we wanted, keep going
                        {
                            BeginInvoke(() =>
                            {
                                MessageBox.Show($"We Found A Match after {currentWarps} attempts! Let's keep going to find more!\r\n\r\n{pk}\r\n", "Found!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            });
                        }
                        else if (matchesFilter && cacheIsFull && action != ShinyFoundAction.StopOnFound) // Found what we wanted in this run and the cache is full
                        {
                            shouldStop = true;
                            stopMessage = $"A Shiny {(pk.PKM.IsAlpha ? "Alpha " : "")}matching the filter has been found after {currentWarps} attempts and your stash is now full!\r\nStopping warping.\r\n\r\n{pk}\r\n";
                        }
                        else if (!matchesFilter && action == ShinyFoundAction.ClearAndContinue) // Does not match filter, clear it out
                        {
                            shouldSendEmbed = false;
                            int index = shinyHunter.StashedShinies.IndexOf(pk);
                            await shinyHunter.ClearSingleFromCache(index, GlobalToken); // keep removing until we have a whole cache of matches
                            shinyHunter.StashedShinies.RemoveAt(index);
                            Invoke(() => StashList[index].Image = null);
                            DisplayStashedShinies();
                        }
                        else if (!matchesFilter && cacheIsFull) // Does not match filter but the cache is full
                        {
                            if (action == ShinyFoundAction.StopAtFullCache) // Maybe something worth catching not being filtered for
                            {
                                shouldStop = true;
                                stopMessage = matchingShinyFound
                                    ? "A shiny matching your filter was found earlier in the hunt.\r\nYour shiny cache is now full!\r\nStopping warping to preserve filtered match."
                                    : "No shiny matching your filter was found.\r\nYour shiny cache is now full!\r\nStopping warping.";
                            }
                            else if (action == ShinyFoundAction.CacheAndContinue && matchingShinyFound) // Most recent does not match filter but the cache is now full and a filter match was found at an earlier point
                            {
                                shouldStop = true;
                                stopMessage = "A shiny matching your filter was found earlier in the hunt.\r\nYour shiny cache is now full!\r\nStopping to preserve your matching shiny.";
                            }
                            else // Cache is full but no match found. Keep going
                            {
                                ShowUnwantedShinyMessage(pk);
                            }
                        }
                        else if (!matchesFilter) // No match, still room in cache
                        {
                            ShowUnwantedShinyMessage(pk);
                        }

                        if (shouldSendEmbed)
                            await SendWebhook(pk.ToShowdownString(), pk.PKM);

                        if (shouldStop)
                        {
                            StopWarping(stopMessage);
                        }
                    }

                    // Helper methods
                    async void StopWarping(string message)
                    {
                        warping = false;
                        await ConnectionWrapper.OpenMenu(GlobalToken);
                        await CleanUpBotAsync();

                        Invoke(() => btnWarp.Text = "Start Warping");
                        SetFiltersEnableState(true);
                        BeginInvoke(() => MessageBox.Show(message, cacheIsFull ? "Cache Full!" : "Found!", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    }

                    void ShowUnwantedShinyMessage(StashedShiny<PA9> pk)
                    {
                        BeginInvoke(() =>
                        {
                            MessageBox.Show($"The following Shiny {(pk.PKM.IsAlpha ? "Alpha " : "")}has been found, but does not match your filter.\r\nYou may wish to remove it such that it doesn't occupy one of your shiny stash slots.\r\n\r\n{pk}\r\n",
                                "Found something we don't want!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                    }
                }

                foreach (var pos in positions)
                {
                    if (!warping)
                        return;
                    await ConnectionWrapper.SetPlayerPosition(pos.X, pos.Y, pos.Z, GlobalToken);
                    await Task.Delay(1_000).ConfigureAwait(false); // fall out and load species
                                                                   // handle falling out
                    int tries = 25;
                    for (; tries > 0; --tries)
                    {
                        if (!warping)
                            break;

                        // check for less than 0.02 difference to avoid float precision issues. We only care about Y here as X/Z may vary due to terrain
                        var position = await ConnectionWrapper.GetPlayerPositionAsync(GlobalToken).ConfigureAwait(false);
                        if (position.Y >= pos.Y - 0.02f && position.Y <= pos.Y + 0.02f)
                            break;
                        await ConnectionWrapper.SetPlayerPosition(pos.X, pos.Y + (tries > 20 ? 1 : 0), pos.Z, GlobalToken);
                        await Task.Delay(1_200).ConfigureAwait(false);
                    }

                    if (tries == 0) // failed to load
                    {
                        warping = false;
                        await CleanUpBotAsync();
                        Invoke(() => btnWarp.Text = "Start Warping");
                        SetFiltersEnableState(true);
                        BeginInvoke(() => MessageBox.Show($"Warping has failed, please check the console!"));
                        break;
                    }

                    if (pos.Flags.Contains("instant"))
                        continue;

                    if (pos.Flags.Contains("halfwait"))
                        await Task.Delay(warpInterval / 2).ConfigureAwait(false);
                    else
                        await Task.Delay(warpInterval).ConfigureAwait(false);
                }
            }
        }

        private void OnSpeciesSelectedIndexChange(object sender, EventArgs e)
        {
            // Update the dictionary when items are checked/unchecked
            if (cBSpecies.SelectedIndex >= 0 && cBSpecies.SelectedIndex < cBSpecies.Items.Count)
            {
                string item = cBSpecies.Items[cBSpecies.SelectedIndex].ToString()!;
                speciesCheckStates[item] = cBSpecies.GetItemChecked(cBSpecies.SelectedIndex);

                // If anything other than "Any" is checked, uncheck "Any"
                if (item != "Any" && cBSpecies.GetItemChecked(cBSpecies.SelectedIndex))
                {
                    if (cBSpecies.Items.Contains("Any"))
                    {
                        int anyIndex = cBSpecies.Items.IndexOf("Any");
                        cBSpecies.SetItemChecked(anyIndex, false);
                    }
                    speciesCheckStates["Any"] = false;
                }
                // If "Any" is checked, uncheck everything else
                else if (item == "Any" && cBSpecies.GetItemChecked(cBSpecies.SelectedIndex))
                {
                    foreach (var key in speciesCheckStates.Keys.ToList())
                    {
                        if (key != "Any")
                            speciesCheckStates[key] = false;
                    }
                    for (int i = 1; i < cBSpecies.Items.Count; i++)
                    {
                        cBSpecies.SetItemChecked(i, false);
                    }
                }

                // Clear search box after checking/unchecking
                tbSpeciesSearch.Text = string.Empty;
            }

            // Select "Any" if nothing is checked
            bool anyChecked = speciesCheckStates.Any(kvp => kvp.Value);
            if (!anyChecked)
            {
                speciesCheckStates["Any"] = true;
                if (cBSpecies.Items.Contains("Any"))
                {
                    int anyIndex = cBSpecies.Items.IndexOf("Any");
                    cBSpecies.SetItemChecked(anyIndex, true);
                }
            }
        }

        private void OnTextChangedSpeciesSearch(object sender, EventArgs e)
        {
            // Save current checked states
            for (int i = 0; i < cBSpecies.Items.Count; i++)
            {
                string item = cBSpecies.Items[i].ToString()!;
                speciesCheckStates[item] = cBSpecies.GetItemChecked(i);
            }

            string searchText = tbSpeciesSearch.Text.ToLower();
            cBSpecies.Items.Clear();

            // Filter and add items keeping current checked state
            foreach (var item in filterSpecies)
            {
                // Just the name part (after " - ") for searching
                string searchablePart = item;
                if (item.Contains(" - "))
                {
                    searchablePart = item.Split(" - ")[1];
                }

                if (searchablePart.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase) || item == "Any")
                {
                    int index = cBSpecies.Items.Add(item);
                    if (speciesCheckStates.TryGetValue(item, out bool value))
                    {
                        cBSpecies.SetItemChecked(index, value);
                    }
                }
            }
        }

        private void OnMouseHover(object sender, EventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            Invoke(() =>
            {
                pb.BorderStyle = BorderStyle.FixedSingle;
                ShinyInfo.Hide(pb);
                ShinyInfo.Active = false;
                ShinyInfo.Active = true;
            });

            int index = StashList.IndexOf(pb);
            if (index >= 0 && index < shinyHunter.StashedShinies.Count)
            {
                if (InvokeRequired)
                    Invoke(() => ShinyInfo.SetToolTip(pb, shinyHunter.StashedShinies[index].ToString()));
                else
                    ShinyInfo.SetToolTip(pb, shinyHunter.StashedShinies[index].ToString());

            }
            else
            {
                if (InvokeRequired)
                    Invoke(() => ShinyInfo.SetToolTip(pb, "No shiny data"));
                else
                    ShinyInfo.SetToolTip(pb, "No shiny data");
            }
        }

        private void ResetSprites()
        {
            Invoke(() =>
            {
                foreach (var pb in StashList)
                    pb.Image = null;
            });
        }

        private void DisplayStashedShinies()
        {
            if (shinyContextMenu == null)
            {
                InitializeShinyContextMenu();
            }

            Invoke(() =>
            {
                // Clear all sprites
                foreach (var pb in StashList)
                    pb.Image = null;

                // Set sprites for stashed shinies
                for (int i = 0; i < shinyHunter.StashedShinies.Count; i++)
                {
                    var pk = shinyHunter.StashedShinies[i].PKM;
                    StashList[i].Image = pk.Sprite();
                }
            });
        }

        private void InitializeShinyContextMenu()
        {
            Invoke(() =>
            {
                shinyContextMenu = new ContextMenuStrip();

                var clearItem = new ToolStripMenuItem("Clear from Stash")
                {
                    Name = "ClearOne"
                };
                clearItem.Click += ClearShinyFromStash;
                shinyContextMenu.Items.Add(clearItem);

                var clearAllItems = new ToolStripMenuItem("Clear ALL from Stash")
                {
                    Name = "ClearAll"
                };
                clearAllItems.Click += ClearStash;
                shinyContextMenu.Items.Add(clearAllItems);

                var refreshCache = new ToolStripMenuItem("Refresh")
                {
                    Name = "Refresh"
                };
                refreshCache.Click += RefreshStash;
                shinyContextMenu.Items.Add(refreshCache);

                var warp = new ToolStripMenuItem("Warp To")
                {
                    Name = "Warp"
                };
                warp.Click += TeleportToSpawner;
                shinyContextMenu.Items.Add(warp);


                shinyContextMenu.Opening += ShinyContextMenuItems;

                foreach (var pb in StashList)
                    pb.ContextMenuStrip = shinyContextMenu;
            });
        }

        private void ShinyContextMenuItems(object? sender, EventArgs e)
        {
            if (sender is not ContextMenuStrip menu)
                return;

            if (menu.SourceControl is not PictureBox pb)
                return;

            int index = StashList.IndexOf(pb);
            bool hasShiny = index >= 0 && index < shinyHunter.StashedShinies.Count;

            if (menu.Items["ClearOne"] is ToolStripMenuItem clearOne)
                clearOne.Visible = hasShiny;

            if (menu.Items["ClearAll"] is ToolStripMenuItem clearAll)
                clearAll.Visible = hasShiny;

            if (menu.Items["Refresh"] is ToolStripMenuItem refresh)
                refresh.Visible = true; // always visible
        }

        private async void ClearShinyFromStash(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem)
                return;
            if (menuItem.Owner is not ContextMenuStrip contextMenu)
                return;
            if (contextMenu.SourceControl is not PictureBox pb)
                return;

            int index = StashList.IndexOf(pb);
            if (index >= 0 && index < shinyHunter.StashedShinies.Count)
            {
                var shiny = shinyHunter.StashedShinies[index];

                DialogResult result = DialogResult.No;
                Invoke(() =>
                {
                    result = MessageBox.Show(
                        $"Are you sure you want to clear {shiny.PKM.Nickname} from the stash?\n\nThis will despawn the Pokémon in-game.",
                        "Confirm Clear",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                });

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await shinyHunter.ClearSingleFromCache(index, GlobalToken);
                        await shinyHunter.LoadStashedShinies(GlobalToken);
                        DisplayStashedShinies();

                        BeginInvoke(() =>
                        {
                            MessageBox.Show(
                                $"{shiny.PKM.Nickname} has been cleared from the stash.",
                                "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke(() =>
                        {
                            MessageBox.Show(
                                $"Error clearing shiny from stash: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        });
                    }
                }
            }
        }

        private async void ClearStash(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem)
                return;
            if (menuItem.Owner is not ContextMenuStrip contextMenu)
                return;
            if (contextMenu.SourceControl is not PictureBox)
                return;

            if (shinyHunter.StashedShinies.Count != 0)
            {
                DialogResult result = DialogResult.No;
                Invoke(() =>
                {
                    result = MessageBox.Show(
                        $"Are you sure you want to clear ALL stashed shinies?\n\nThis will despawn all current shinies in-game.",
                        "Confirm Clear",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                });

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await shinyHunter.ClearAllFromCache(GlobalToken);
                        await shinyHunter.LoadStashedShinies(GlobalToken);
                        DisplayStashedShinies();

                        BeginInvoke(() =>
                        {
                            MessageBox.Show(
                                $"All Pokémon have been cleared from the stash.",
                                "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke(() =>
                        {
                            MessageBox.Show(
                                $"Error clearing shiny from stash: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        });
                    }
                }
            }
        }

        private async void RefreshStash(object? sender, EventArgs e)
        {
            // Get the picture box that was right-clicked
            if (sender is not ToolStripMenuItem menuItem)
                return;
            if (menuItem.Owner is not ContextMenuStrip contextMenu)
                return;
            if (contextMenu.SourceControl is not PictureBox)
                return;
            try
            {
                // Reload the stashed shinies
                await shinyHunter.LoadStashedShinies(GlobalToken);
                // Update the display
                DisplayStashedShinies();
                BeginInvoke(() =>
                {
                    MessageBox.Show(ActiveForm, $"Shiny stash has been refreshed.", "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                BeginInvoke(() =>
                {
                    MessageBox.Show($"Error refreshing shiny stash: {ex.Message}", "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                });
            }
        }

        private async void TeleportToSpawner(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem)
                return;
            if (menuItem.Owner is not ContextMenuStrip contextMenu)
                return;
            if (contextMenu.SourceControl is not PictureBox pb)
                return;
            if (warping)
                return;

            int index = StashList.IndexOf(pb);
            var shiny = shinyHunter.StashedShinies[index];

            if (LocationParser.MainSpawnerCoordinates != null && LocationParser.MainSpawnerCoordinates.TryGetValue($"{shiny.LocationHash:X16}", out var location))
            {
                var result = MessageBox.Show($"You must be on the *Main Map* to warp to this Shiny's location{Environment.NewLine}Click OK to proceed", "Map Required", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result == DialogResult.OK)
                {
                    var adjusted = new Vector3 { X = location.X + .05f, Y = location.Y + .5f, Z = location.Z + .05f };
                await TeleportPlayer(adjusted, true);
                }
                else
                {
                    return;
                }
            }
            else if (LocationParser.SewersSpawnerCoordinates != null && LocationParser.SewersSpawnerCoordinates.TryGetValue($"{shiny.LocationHash:X16}", out location))
            {
                var result = MessageBox.Show($"You must be in *The Sewers* to warp to this Shiny's location{Environment.NewLine}Click OK to proceed", "Map Required", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result == DialogResult.OK)
                {
                    var adjusted = new Vector3 { X = location.X , Y = location.Y + .5f, Z = location.Z };
                    await TeleportPlayer(adjusted, true);
                }
                else
                {
                    return;
                }
            }
            else if (LocationParser.LysandreSpawnerCoordinates != null && LocationParser.LysandreSpawnerCoordinates.TryGetValue($"{shiny.LocationHash:X16}", out location))
            {
                var result = MessageBox.Show($"You must be in *Lysandre Labs* to warp to this Shiny's location{Environment.NewLine}Click OK to proceed", "Map Required", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (result == DialogResult.OK)
                {
                    var adjusted = new Vector3 { X = location.X, Y = location.Y + 5f, Z = location.Z };
                    await TeleportPlayer(adjusted, true);
                }
                else
                {
                    return;
                }
            }
            else
            {
                BeginInvoke(() =>
                {
                    MessageBox.Show($"Unable to determine spawn location", "Unable to find!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
            }
        }

        private void OnClickExportSets(object sender, EventArgs e)
        {
            if (shinyHunter.StashedShinies.Count == 0)
            {
                BeginInvoke(() =>
                {
                    MessageBox.Show("Nothing to export",
                                    "Export",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                });
                return;
            }

            BeginInvoke(() =>
            {
                var result = MessageBox.Show("Export the currently stashed shiny Pokémon to Showdown format?",
                                             "Export Confirmation",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var exportPath = Path.GetFullPath("Sets.txt");
                    File.WriteAllText(exportPath, shinyHunter.GetShinyStashInfo(shinyHunter.StashedShinies));
                    MessageBox.Show($"Export successful!\n\nSaved to:\n{exportPath}",
                                    "Export Complete",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            });
        }

        private async Task SendWebhook(string showdownSet, PA9 pk)
        {
            if (programConfig.Webhooks == null || programConfig.Webhooks.Count == 0)
                return;

            var strings = GameInfo.GetStrings("en");
            var species = strings.Species[pk.Species];
            var formName = ShowdownParsing.GetStringFromForm(pk.Form, strings, pk.Species, pk.Context);
            if (!string.IsNullOrEmpty(formName))
                species += $"-{formName}";
            string imageUrl = $"https://raw.githubusercontent.com/Omni-KingZeno/Pokemon-Sprites/refs/heads/main/Shiny/{species.ToLower().Replace("é", "e")}.png";

            var embed = new
            {
                title = $"{(pk.IsAlpha ? "Alpha " : "")}Shiny {species} Found!",
                description = showdownSet,
                image = new { url = imageUrl.Trim() },
                color = pk.PersonalInfo.Color
            };

            var failedPosts = new List<string>();

            foreach (var webhook in programConfig.Webhooks)
            {
                if (!webhook.Enabled || string.IsNullOrWhiteSpace(webhook.WebhookAddress))
                    continue;

                try
                {
                    string json;

                    // Use custom message content if provided
                    if (!string.IsNullOrWhiteSpace(webhook.MessageContents))
                    {
                        var payloadWithContent = new
                        {
                            content = webhook.MessageContents,
                            embeds = new[] { embed }
                        };
                        json = JsonSerializer.Serialize(payloadWithContent);
                    }
                    else
                    {
                        var payloadNoContent = new
                        {
                            embeds = new[] { embed }
                        };
                        json = JsonSerializer.Serialize(payloadNoContent);
                    }

                    var response = await httpClient.PostAsync(webhook.WebhookAddress.Trim(),
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex)
                {
                    failedPosts.Add($"{webhook.WebhookAddress}: {ex.Message}");
                }
            }

            if (failedPosts.Count > 0)
            {
                BeginInvoke(() =>
                {
                    MessageBox.Show($"Failed to send {failedPosts.Count} webhook(s):\n\n{string.Join("\n", failedPosts)}",
                                "Webhook Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                });
            }
        }

        private void LegacyImport()
        {
            try
            {
                const string legacyPositions = "positions.txt";
                const string legacyFilter = "filter_config.txt";

                if (File.Exists(legacyPositions))
                {
                    var lines = File.ReadAllLines(legacyPositions);
                    foreach (var line in lines)
                        positions.Add(Vector3.FromString(line));
                }

                if (File.Exists(legacyFilter))
                {
                    var importedIndices = new List<int>();
                    var lines = File.ReadAllLines(legacyFilter);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("SpeciesIndices="))
                        {
                            var indicesStr = line["SpeciesIndices=".Length..];
                            if (!string.IsNullOrEmpty(indicesStr))
                            {
                                importedIndices = [.. indicesStr.Split(',')
                                    .Select(s => int.TryParse(s.Trim(), out int idx) ? idx : -1)
                                    .Where(idx => idx >= 0)];
                            }
                            break;
                        }
                    }

                    if (importedIndices.Count > 0)
                    {
                        for (int i = 0; i < cBSpecies.Items.Count; i++)
                        {
                            Invoke(() => cBSpecies.SetItemChecked(i, importedIndices.Contains(i)));
                        }
                    }
                }

                UpdateUI();
            }
            catch
            {
                // Silently fail if we can't import
            }
        }

        private void OnClickWebhookSettings(object sender, EventArgs e)
        {
            var form = new WebhookForm(programConfig);
            CenterFormOnParent(form);
            if (form.ShowDialog() == DialogResult.OK)
            {
                SaveConfig();
            }
        }

        private void SetupListBox()
        {
            ContextMenuStrip contextMenu = new();

            ToolStripMenuItem copyItem = new("Copy");
            copyItem.Click += CopyLineItem;
            contextMenu.Items.Add(copyItem);

            ToolStripMenuItem pasteAboveItem = new("Insert Above");
            pasteAboveItem.Click += PasteAboveItem;
            contextMenu.Items.Add(pasteAboveItem);

            ToolStripMenuItem pasteBelowItem = new("Insert Below");
            pasteBelowItem.Click += PasteBelowItem;
            contextMenu.Items.Add(pasteBelowItem);

            lBCoords.ContextMenuStrip = contextMenu;
            lBCoords.MouseDown += CoordsRightClick;
        }

        private void CopyLineItem(object? sender, EventArgs e)
        {
            if (lBCoords.SelectedItem != null)
            {
                string? lineToCopy = lBCoords.SelectedItem.ToString();
                Clipboard.SetText(lineToCopy);
            }
        }

        private void PasteAboveItem(object? sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                try
                {
                    string clipboardText = Clipboard.GetText().Trim();
                    Vector3 vector = Vector3.FromString(clipboardText);

                    int insertIndex = lBCoords.SelectedIndex;
                    Invoke(() => lBCoords.Items.Insert(insertIndex, vector));
                    positions.Insert(insertIndex, vector);
                    SaveAllAndUpdateUI();
                    Invoke(() => lBCoords.SelectedIndex = insertIndex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error pasting item: {ex.Message}",
                        "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No text in clipboard to paste.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PasteBelowItem(object? sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                try
                {
                    string clipboardText = Clipboard.GetText().Trim();
                    Vector3 vector = Vector3.FromString(clipboardText);

                    int insertIndex = lBCoords.SelectedIndex;
                    Invoke(() => lBCoords.Items.Insert(insertIndex + 1, vector));
                    positions.Insert(insertIndex + 1, vector);
                    SaveAllAndUpdateUI();
                    Invoke(() => lBCoords.SelectedIndex = insertIndex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error pasting item: {ex.Message}",
                        "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No text in clipboard to paste.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CoordsRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lBCoords.IndexFromPoint(e.Location);
                if (index >= 0 && index < lBCoords.Items.Count)
                {
                    Invoke(() => lBCoords.SelectedIndex = index);
                }
            }
        }

        private void OnClickMonitoring(object sender, EventArgs e)
        {
            Invoke(() =>
            {
                if (monitoring)
                {
                    monitoring = false;
                    btnMonitoring.Text = "Start Monitoring";
                    btnWarp.Enabled = true;
                    _monitoringCts?.Cancel();
                }
                else
                {
                    monitoring = true;
                    btnWarp.Enabled = false;
                    btnMonitoring.Text = "Monitoring. Click to end";
                    _monitoringCts = new CancellationTokenSource();
                    _ = MonitorStash(_monitoringCts.Token);
                }
            });
        }

        private async Task MonitorStash(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Refresh stashed shinies
                    var newFound = await shinyHunter.LoadStashedShinies(GlobalToken);
                    if (newFound)
                    {
                        DisplayStashedShinies();
                        var newShinies = shinyHunter.DifferentShinies;
                        foreach (var pk in newShinies)
                        {
                            await SendWebhook(pk.ToShowdownString(), pk.PKM);
                            BeginInvoke(() =>
                            {
                                MessageBox.Show($"The following Shiny {(pk.PKM.IsAlpha ? "Alpha " : "")}has been found.\r\n\r\n{pk}\r\n",
                                    "Found something new!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            });
                        }
                    }

                    // Check every minute for new stashed shinies
                    await Task.Delay(60000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error monitoring stash: {ex.Message}");
                    await Task.Delay(1000, token).ConfigureAwait(false);
                }
            }
        }
    }

    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string[] Flags { get; set; }

        public Vector3()
        {
            X = 0; Y = 0; Z = 0;
            Flags = [];
        }

        public override readonly string ToString()
        {
            return $"{X},{Y},{Z};{string.Join(',', Flags)}";
        }

        public static Vector3 FromString(string s)
        {
            var prePostFlags = s.Split(';');
            var spl = prePostFlags[0].Split(',');
            Vector3 v = new()
            {
                X = float.Parse(spl[0]),
                Y = float.Parse(spl[1]),
                Z = float.Parse(spl[2])
            };

            if (prePostFlags.Length > 1)
            {
                var nFlags = new List<string>();
                spl = prePostFlags[1].Split(',');
                foreach (var str in spl)
                    nFlags.Add(str);
                v.Flags = [.. nFlags];
            }

            return v;
        }
    }
}