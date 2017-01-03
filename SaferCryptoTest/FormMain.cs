using System;
using System.Windows.Forms;

namespace SaferCrypto.Test {
    public partial class FormMain : Form {
        private const string MasterKey = "MasterKey";

        public FormMain() {
            this.InitializeComponent();
            this.txtMasterKey.Text = MasterKey;
        }

        private void btnEncrypt_Click(object sender, EventArgs e) {
            var crypto = new SaferCrypto();

            var encrypted = crypto.Encrypt<string>(this.txtOriginal.Text, this.txtMasterKey.Text);

            this.txtEncrypted.Text = encrypted;
        }

        private void btnDecrypt_Click(object sender, EventArgs e) {
            try {
                var crypto = new SaferCrypto();
                var decrypted = crypto.Decrypt<string>(this.txtEncrypted.Text, this.txtMasterKey.Text);

                this.txtDecrypted.Text = decrypted;
            }
            catch (SafeCryptoException ex) {
                MessageBox.Show(ex.ToString());
            }
            catch (FormatException fex) {
                MessageBox.Show("Format exception - oh well");
            }
        }
    }
}