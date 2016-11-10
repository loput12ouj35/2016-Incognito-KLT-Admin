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

namespace GP
{
    /// <summary>
    /// Interaction logic for Preset.xaml
    /// </summary>
    public partial class Preset : Window
    {
        private readonly DataManager dm;
        private readonly List<PreXML> list;

        private class PreXML
        {
            public readonly string name;
            public readonly string path;

            public PreXML(string name = "noname", string path = "")
            {
                this.name = name;
                this.path = AppDomain.CurrentDomain.BaseDirectory + @"Preset\" + path;
            }

            public override string ToString()
            {
                return name;
            }
        }

        public Preset()
        {
            InitializeComponent();

            dm = DataManager.GetDataManager();
            list = new List<PreXML>();

            //리스트 채우기
            list.Add(new PreXML("나이", "age.xml"));

            listBox.DataContext = null;
            listBox.DataContext = list;
        }

        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            if(listBox.SelectedIndex >= 0)
            {
                dm.SavePresetPath(list[listBox.SelectedIndex].path);
            }

            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
