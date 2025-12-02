using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using PKHeX.Core;
using SysBot.Base;

namespace ZAShinyWarper
{
    public partial class SettingsForm : Form
    {
        private readonly ProgramConfig _config;

        public SettingsForm(ProgramConfig config)
        {
            InitializeComponent();

            foreach (var item in Enum.GetValues<SwitchProtocol>())
                cBProtocol.Items.Add(item);

            _config = config;
            LoadSettings();
        }

        private void LoadSettings()
        {
            dGWebhookSettings.Rows.Clear();

            // Load config
            if (_config.Webhooks != null && _config.Webhooks.Count > 0)
            {
                foreach (var webhook in _config.Webhooks)
                {
                    dGWebhookSettings.Rows.Add(
                       webhook.WebhookAddress,
                       webhook.MessageContents,
                       webhook.Enabled
                   );
                }
            }
            else
            {
                dGWebhookSettings.Rows.Add("", "", false);
            }

            tB_IP.Text = _config.IPAddress;
            tB_Port.Text = _config.UsbPort.ToString();
            cBProtocol.SelectedItem = _config.Protocol;
            cBMatchNotify.Checked = _config.NotifyOnMatch;
            cBFilterNonMatch.Checked = _config.NotifyOnNonMatch;
            cBFullCache.Checked = _config.NotifyOnCacheFull;
        }

        private void SaveSettings()
        {
            _config.Webhooks.Clear();

            foreach (DataGridViewRow row in dGWebhookSettings.Rows)
            {
                string webhookAddr = row.Cells["webhookColumn"].Value?.ToString() ?? "";
                string message = row.Cells["messageColumn"].Value?.ToString() ?? "";
                bool enabled = row.Cells["checkColumn"].Value as bool? ?? false;

                // Only save rows that have at least a webhook address
                if (!string.IsNullOrWhiteSpace(webhookAddr))
                {
                    _config.Webhooks.Add(new WebhookData
                    {
                        WebhookAddress = webhookAddr,
                        MessageContents = message,
                        Enabled = enabled
                    });
                }
            }
            _config.IPAddress = tB_IP.Text;
            if (int.TryParse(tB_Port.Text, out int port))
            {
                _config.UsbPort = port;
            }
            _config.Protocol = (SwitchProtocol)cBProtocol.SelectedItem!;
            _config.NotifyOnMatch = cBMatchNotify.Checked;
            _config.NotifyOnNonMatch = cBFilterNonMatch.Checked;
            _config.NotifyOnCacheFull = cBFullCache.Checked;
        }

        private void OnClickAdd(object sender, EventArgs e)
        {
            int newRowIndex = dGWebhookSettings.Rows.Add("", "", false);

            // Lock the new row's height
            dGWebhookSettings.Rows[newRowIndex].Height = 25;
            dGWebhookSettings.Rows[newRowIndex].Resizable = DataGridViewTriState.False;
        }

        private void OnClickRemove(object sender, EventArgs e)
        {
            if (dGWebhookSettings.CurrentRow != null && dGWebhookSettings.CurrentRow.Index >= 0)
            {
                dGWebhookSettings.Rows.RemoveAt(dGWebhookSettings.CurrentRow.Index);
            }
        }

        private void OnClickSave(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnClickCancel(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnClickTest(object sender, EventArgs e)
        {
            if (dGWebhookSettings.CurrentRow != null && dGWebhookSettings.CurrentRow.Index >= 0)
            {
                string webhookAddr = dGWebhookSettings.CurrentRow.Cells["webhookAddress"].Value?.ToString() ?? "";
                string message = dGWebhookSettings.CurrentRow.Cells["messageContents"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(webhookAddr))
                {
                    MessageBox.Show("Please enter a valid webhook address to test.", "Invalid Webhook", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    var pk = LoadPA9();
                    using HttpClient client = new();
                    string imageUrl = $"https://raw.githubusercontent.com/Omni-KingZeno/Pokemon-Sprites/refs/heads/main/Shiny/zygarde-10.png";

                    var embed = new
                    {
                        title = $"Shiny Alpha Zygarde Found!?!?",
                        description = ShowdownParsing.GetShowdownText(pk),
                        image = new { url = imageUrl.Trim() },
                        footer = new
                        {
                            text = "This is only a test",
                        }
                    };
                    var payload = new
                    {
                        content = message,
                        embeds = new[] { embed }
                    };
                    var json = JsonSerializer.Serialize(payload);


                    var response = client.PostAsJsonAsync(webhookAddr, payload).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Test webhook sent successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to send test webhook. Status Code: {response.StatusCode}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while sending the test webhook:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static PA9 LoadPA9()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ZAShinyWarper.Resources.test.pa9";
            using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Resource {resourceName} not found");
            byte[] data = new byte[stream.Length];
            stream.ReadExactly(data);
            return new PA9(data);
        }
    }
}