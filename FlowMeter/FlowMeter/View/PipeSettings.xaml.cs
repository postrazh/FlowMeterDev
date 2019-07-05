using FlowMeter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlowMeter.View
{
    /// <summary>
    /// Interaction logic for PipeSettings.xaml
    /// </summary>
    public partial class PipeSettings : Window
    {
        public PipeSettings()
        {
            InitializeComponent();
        }

        private void TextBox_DecimalOnly(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]*(?:\.[0-9]*)?$");
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // asset names
            txtAsset01Name.Text = Settings.Asset01Name;
            txtAsset02Name.Text = Settings.Asset02Name;
            txtAsset03Name.Text = Settings.Asset03Name;
            txtAsset04Name.Text = Settings.Asset04Name;
            txtAsset05Name.Text = Settings.Asset05Name;
            txtAsset06Name.Text = Settings.Asset06Name;
            txtAsset07Name.Text = Settings.Asset07Name;

            // asset values
            txtAsset01Value.Text = Settings.Asset01Value.ToString();
            txtAsset02Value.Text = Settings.Asset02Value.ToString();
            txtAsset03Value.Text = Settings.Asset03Value.ToString();
            txtAsset04Value.Text = Settings.Asset04Value.ToString();
            txtAsset05Value.Text = Settings.Asset05Value.ToString();
            txtAsset06Value.Text = Settings.Asset06Value.ToString();
            txtAsset07Value.Text = Settings.Asset07Value.ToString();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            // asset names
            Settings.Asset01Name = txtAsset01Name.Text;
            Settings.Asset02Name = txtAsset02Name.Text;
            Settings.Asset03Name = txtAsset03Name.Text;
            Settings.Asset04Name = txtAsset04Name.Text;
            Settings.Asset05Name = txtAsset05Name.Text;
            Settings.Asset06Name = txtAsset06Name.Text;
            Settings.Asset07Name = txtAsset07Name.Text;

            // asset values
            Settings.Asset01Value = Double.Parse(txtAsset01Value.Text);
            Settings.Asset02Value = Double.Parse(txtAsset02Value.Text);
            Settings.Asset03Value = Double.Parse(txtAsset03Value.Text);
            Settings.Asset04Value = Double.Parse(txtAsset04Value.Text);
            Settings.Asset05Value = Double.Parse(txtAsset05Value.Text);
            Settings.Asset06Value = Double.Parse(txtAsset06Value.Text);
            Settings.Asset07Value = Double.Parse(txtAsset07Value.Text);
        }
    }
}
