using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MP_Fenster_App
{
    public partial class TechnologAkceptacja : Window
    {
        private PozycjaZlecenia _pos;
        public string Uwagi { get; private set; }

        public TechnologAkceptacja(PozycjaZlecenia pos)
        {
            InitializeComponent();
            _pos = pos;
            TxtOpisBledu.Text = "Przekroczono limit wymiarów dla: " + pos.SystemOkna;
        }

        private void BtnZatwierdz_Click(object sender, RoutedEventArgs e)
        {
            _pos.CzyZatwierdzone = true;
            _pos.UwagiTechnologa = TxtUwagi.Text;
            this.DialogResult = true; // Zamyka z sukcesem
        }
    }
}
