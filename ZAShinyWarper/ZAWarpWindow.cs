using NHSE.Injection;
using PKHeX.Core;
using PLAWarper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLADumper
{
    public partial class ZAWarpWindow : Form
    {
        private readonly long[] jumpsPos = new long[] { 0x41EC340, 0x248, 0x00, 0x138 }; // [[[[main+41EC340]+248]+00]+138]+90
        private static IRAMReadWriter bot = default!;

        private List<Vector3> positions = new List<Vector3>();
        private const string configName = "positions.txt";

        private ShinyHunter<PK9> shinyHunter = new ShinyHunter<PK9>();

        private LabelForm lf = new LabelForm();

        public ComboBox[] CBIVs = default!;

        // Bot
        private bool warping = false;
        private int warpsPerSave = 10;
        private int currentWarps = 0;

        public ZAWarpWindow()
        {
            InitializeComponent();
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            Application.ApplicationExit += (s, e) => cleanUpBot();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Load enums into comboboxes
            // WhenShinyFound
            foreach (var item in Enum.GetValues<ShinyFoundAction>())
                cBWhenShinyFound.Items.Add(item);
            cBWhenShinyFound.SelectedIndex = 0;

            // Species
            cBSpecies.Items.Add("Any");
            foreach (var item in ZAZukan.PokedexNumbersZA)
                cBSpecies.Items.Add($"{item.Value:D3} - {item.Key}");
            cBSpecies.SelectedIndex = 0;

            // IVs
            CBIVs = new ComboBox[6]
            {
                cBIVHP,
                cBIVAtk,
                cBIVDef,
                cBIVSpA,
                cBIVSpD,
                cBIVSpe
            };

            foreach (var cb in CBIVs)
            {
                foreach (var item in Enum.GetValues<IVType>())
                    cb.Items.Add(item);
                cb.SelectedIndex = 0;
            }

            if (File.Exists("config.txt"))
            {
                var ip = File.ReadAllText("config.txt");
                textBox1.Text = ip;
            }

            LoadAllAndUpdateUI();
        }

        private ShinyHunter<PK9>.ShinyFilter<PK9> getFilter()
        {
            var filter = new ShinyHunter<PK9>.ShinyFilter<PK9>();
            // Species
            if (cBSpecies.SelectedIndex > 0)
            {
                var sel = cBSpecies.SelectedItem.ToString()!;
                var spl = sel.Split(" - ");
                filter.Species = ushort.Parse(spl[0]);
            }

            // IVs
            for (int i = 0; i < 6; i++)
            {
                filter.IVs[i] = (IVType)CBIVs[i].SelectedItem;
            }

            // Size
            filter.SizeMinimum = (byte)numericUpDownScale.Value;
            return filter;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var botsys = new SysBot();
                botsys.Connect(textBox1.Text, 6000);
                bot = botsys;
                groupBox1.Enabled = true;
                shinyHunter.LoadStashedShinies(bot, "sets.txt");
                MessageBox.Show($"Connected to SysBot (network). The following shinies are stashed on your save currently: \r\n{shinyHunter.GetShowdownSets(shinyHunter.StashedShinies)}");
                bot.SendBytes(Encoding.ASCII.GetBytes("detachController\r\n"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                var botusb = new USBBot();
                botusb.Connect();
                bot = botusb;
                groupBox1.Enabled = true;
                shinyHunter.LoadStashedShinies(bot, "sets.txt");
                MessageBox.Show($"Connected to UsbBot (USB). The following shinies are stashed on your save currently: \r\n{shinyHunter.GetShowdownSets(shinyHunter.StashedShinies)}");
                bot.SendBytes(Encoding.ASCII.GetBytes("detachController\r\n"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            File.WriteAllText("config.txt", textBox1.Text);
        }

        private void MovePlayer(float x, float y)
        {
            int stepOffset = (int)numericUpDown1.Value;
            ulong ramOffset = getPcoordOfs();

            var bytes = bot.ReadBytes(ramOffset, 12, RWMethod.Absolute);
            float xn = BitConverter.ToSingle(bytes, 0);
            float yn = BitConverter.ToSingle(bytes, 8);
            xn += (x * stepOffset); yn += (y * stepOffset);

            bot.WriteBytes(BitConverter.GetBytes(xn), ramOffset, RWMethod.Absolute);
            bot.WriteBytes(BitConverter.GetBytes(yn), ramOffset + 8, RWMethod.Absolute);
        }

        private Vector3 GetPos()
        {
            ulong ramOffset = getPcoordOfs();
            var bytes = bot.ReadBytes(ramOffset, 12, RWMethod.Absolute);

            float xn = BitConverter.ToSingle(bytes, 0);
            float yn = BitConverter.ToSingle(bytes, 4);
            float zn = BitConverter.ToSingle(bytes, 8);

            return new Vector3() { x = xn, y = yn, z = zn };
        }

        private void SetPos(float x, float y, float z)
        {
            ulong ramOffset = getPcoordOfs();

            byte[] xb = BitConverter.GetBytes(x);
            byte[] yb = BitConverter.GetBytes(y);
            byte[] zb = BitConverter.GetBytes(z);

            var bytes = xb.Concat(yb).Concat(zb);

            bot.WriteBytes(bytes.ToArray(), ramOffset, RWMethod.Absolute);
        }

        private ulong getPcoordOfs()
        {
            return bot.FollowMainPointer(jumpsPos) + 0x90;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MovePlayer(0, 1);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            MovePlayer(0, -1);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MovePlayer(1, 0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            MovePlayer(-1, 0);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            SaveNewValue();
        }

        private void SaveNewValue()
        {
            var pos = GetPos();
            positions.Add(pos);
            SaveAllAndUpdateUI();

            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void LoadAllAndUpdateUI()
        {
            if (File.Exists(configName))
            {
                var lines = File.ReadAllLines(configName);
                foreach (var line in lines)
                    positions.Add(Vector3.FromString(line));
            }

            UpdateUI();
        }

        private void SaveAllAndUpdateUI()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pos in positions)
                sb.AppendLine(pos.ToString());

            File.WriteAllText(configName, sb.ToString());

            UpdateUI();
        }

        private void UpdateUI()
        {
            listBox1.SelectedIndex = -1;
            listBox1.Items.Clear();
            foreach (var pos in positions)
                listBox1.Items.Add(pos);
        }

        private async void button3_Click_1(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                var toSend = (Vector3)listBox1.SelectedItem;
                SetPos(toSend.x, toSend.y, toSend.z);
                Thread.Sleep(4000); // fall out

                int i;
                lf.PerformSafely(() => lf.SetText("Warping..."));
                lf.PerformSafely(() => lf.Show());

                for (i = 0; i < 15; ++i)
                {
                    lf.PerformSafely(() => lf.SetText($"Warping... ({i + 1}/15) Please wait."));
                    if (GetPos().y == toSend.y)
                        break;
                    SetPos(toSend.x, toSend.y, toSend.z);
                    await Task.Delay(1100).ConfigureAwait(false);
                }

                lf.PerformSafely(() => lf.Hide());
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                positions.RemoveAt(listBox1.SelectedIndex);
                SaveAllAndUpdateUI();
            }
        }

        private void setFiltersEnableState(bool enabled)
        {
            cBSpecies.Enabled = enabled;
            foreach (var cb in CBIVs)
                cb.Enabled = enabled;
            numericUpDownScale.Enabled = enabled;
            numericUpDownSpawnCheckTime.Enabled = enabled;
            cBWhenShinyFound.Enabled = enabled;
            numericUpDownCamMove.Enabled = enabled;
        }

        // Bot

        private void cleanUpBot()
        {
            if (bot != null && bot.Connected)
                bot.SendBytes(Encoding.ASCII.GetBytes("setStick RIGHT 0 0\r\n"));
        }

        private async void btnWarp_Click(object sender, EventArgs e)
        {
            if (warping)
            {
                warping = false;
                setFiltersEnableState(true);
                btnWarp.PerformSafely(() => btnWarp.Text = "Start Warping");
                cleanUpBot();
                return;
            }

            if (positions.Count < 2)
            {
                MessageBox.Show("Not enough postions have been set to warp to!");
                return;
            }

            var fitler = getFilter();
            var warpInterval = (int)numericUpDownSpawnCheckTime.Value;
            var camSpeed = (int)numericUpDownCamMove.Value;
            var action = (ShinyFoundAction)cBWhenShinyFound.SelectedItem!;
            currentWarps = 0;

            warping = true;
            setFiltersEnableState(false);
            btnWarp.PerformSafely(() => btnWarp.Text = "Warping. Click to end.");

            // Rotate camera for spawns
            if (camSpeed != 0)
                bot.SendBytes(Encoding.ASCII.GetBytes($"setStick RIGHT {camSpeed} 0\r\n"));

            while (warping)
            {
                currentWarps++;
                if (currentWarps % warpsPerSave == 0)
                {
                    // Save
                    bot.SendBytes(Encoding.ASCII.GetBytes("click X\r\n"));
                    await Task.Delay(1_000).ConfigureAwait(false);
                    bot.SendBytes(Encoding.ASCII.GetBytes("click R\r\n"));
                    await Task.Delay(1_000).ConfigureAwait(false);
                    bot.SendBytes(Encoding.ASCII.GetBytes("click A\r\n"));
                    await Task.Delay(5_000).ConfigureAwait(false); // wait for save
                    bot.SendBytes(Encoding.ASCII.GetBytes("click B\r\n"));
                    await Task.Delay(0_800).ConfigureAwait(false);
                    bot.SendBytes(Encoding.ASCII.GetBytes("click B\r\n"));
                    await Task.Delay(0_800).ConfigureAwait(false);
                }

                // Check shinies first as a new one may have spawned before we move
                var newFound = shinyHunter.LoadStashedShinies(bot, "sets.txt");
                if (newFound)
                {
                    var newShinies = shinyHunter.DifferentShinies;
                    foreach (var pk in newShinies)
                    {
                        if (fitler.MatchesFilter(pk))
                        {
                            // Found one
                            switch (action)
                            {
                                case ShinyFoundAction.StopOnFound:
                                    warping = false;
                                    cleanUpBot();
                                    bot.SendBytes(Encoding.ASCII.GetBytes("click X\r\n"));
                                    btnWarp.PerformSafely(() => btnWarp.Text = "Start Warping");
                                    MessageBox.Show($"A shiny matching the filter has been found! Stopping warping.\r\n\r\n{ShowdownParsing.GetShowdownText(pk)}");
                                    break;
                                    //case ShinyFoundAction.CacheAndContinue:
                                    //    MessageBox.Show($"A shiny matching the filter has been found!\r\n\r\n{pk.GetShowdownSet()}");
                                    //    break;
                            }
                        }
                    }
                }

                foreach (var pos in positions)
                {
                    if (!warping)
                        break;
                    SetPos(pos.x, pos.y, pos.z);
                    await Task.Delay(4000).ConfigureAwait(false); // fall out and load species
                    // handle falling out
                    int tries = 25;
                    for (; tries > 0; --tries)
                    {
                        if (GetPos().y == pos.y)
                            break;
                        SetPos(pos.x, pos.y, pos.z);
                        await Task.Delay(1100).ConfigureAwait(false);
                    }

                    if (tries == 0) // failed to load
                    {
                        warping = false;
                        cleanUpBot();
                        btnWarp.PerformSafely(() => btnWarp.Text = "Start Warping");
                        MessageBox.Show($"Warping has failed, please check the console!");
                        break;
                    }
                }

                await Task.Delay(warpInterval).ConfigureAwait(false);
            }
        }
    }

    public struct Vector3
    {
        public float x, y, z;

        public override string ToString()
        {
            return $"{x},{y},{z}";
        }

        public static Vector3 FromString(string s)
        {
            var spl = s.Split(',');
            Vector3 v = new Vector3();
            v.x = float.Parse(spl[0]);
            v.y = float.Parse(spl[1]);
            v.z = float.Parse(spl[2]);

            return v;
        }
    }
}
