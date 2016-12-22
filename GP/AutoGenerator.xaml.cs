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
    /// Interaction logic for AutoGenerator.xaml
    /// </summary>
    public partial class AutoGenerator : Window
    {
        public enum AutoType { dyn, stt, hyb, heu }
        
        private AutoType autoType;
        private int autoDepth;
        
        public AutoGenerator()
        {
            InitializeComponent();
            
            radioButtonType0A.IsChecked = true;     //기본 값
            autoDepth = 5;      //기본 값
            slider.Value = autoDepth;            
        }

        private void radioButtonTypeA_Checked(object sender, RoutedEventArgs e)
        {

            string tag = (sender as RadioButton).Tag.ToString();

            switch (tag)
            {
                case "dyn":
                    autoType = AutoType.dyn;
                    break;
                case "stt":
                    autoType = AutoType.stt;
                    break;
                case "hyb":
                    autoType = AutoType.hyb;
                    break;
                case "heu":
                    autoType = AutoType.heu;
                    break;
            }
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                autoDepth = (int)(sender as Slider).Value;
                textBlockDepthA.Text = "트리 깊이: " + autoDepth.ToString();
            }
        }

        private void buttonGenerateA_Click(object sender, RoutedEventArgs e)
        {
            (Owner as XmlWindow).autoMakeTree(autoType, autoDepth - 1, (bool) checkBoxA.IsChecked);
                
        }

        private void buttonCancelA_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
