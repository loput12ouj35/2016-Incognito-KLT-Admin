using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DataManager dm;     //데이터 처리 매니저

        public MainWindow()
        {
            InitializeComponent();

            dm = DataManager.GetDataManager();
        }
        


        /* 
          상단 메뉴 파트
        */
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "txt";
            dialog.Filter = "CSV files(*.txt;*.csv)|*txt;*csv";
            dialog.Multiselect = false;

            string tmpPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            dialog.InitialDirectory = tmpPath;
            dialog.ShowDialog();
            

            if (dialog.FileName != "")
            {
                (sender as MenuItem).IsEnabled = false;     //열기버튼 잠금

                dm.OpenFile(dialog.FileName);
                dataGrid1.DataContext = null;       //초기화
                dataGrid1.DataContext = dm.GetDataTable().DefaultView;      //데이터를 테이블에 로드
                listBox1.DataContext = null;
                listBox1.DataContext = dm.GetAttrList();


                //파일 주소들 로드 및 enable
                textBoxInputFile.Text = "./" + System.IO.Path.GetFileName(dialog.FileName);
                textBoxOutputFile.Text = textBoxInputFile.Text + "_output.txt";
                textBoxLogFile.Text = textBoxInputFile.Text + "_log.txt";

                grid1.IsEnabled = true;
                checkbox0.IsChecked = true;
                checkbox1.IsChecked = true;
            }
        }

        private void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig("config.xml");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Filter = "Xml File|*.xml";
            dialog.Title = "Config 파일 저장";
            dialog.DefaultExt = "xml";
            dialog.AddExtension = true;

            string tmpPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            dialog.InitialDirectory = tmpPath;

            dialog.ShowDialog();

            if(dialog.FileName != "")
            {
                SaveConfig(dialog.FileName);
            }
        }

        private void SaveConfig(string fileName)
        {
            int k, l, t;
            k = int.TryParse(textBoxK.Text, out k) ? k : 0;
            l = int.TryParse(textBoxL.Text, out l) ? l : 0;
            t = int.TryParse(textBoxT.Text, out t) ? t : 0;

            //config 파일 저장
            dm.SaveConfigXML(fileName,
            new Config(k, l, (float)t / 100, textBoxInputFile.Text, textBoxOutputFile.Text, textBoxLogFile.Text));
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "Inco_cpu.exe";
            p.Start();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        /*
            속성 탭 파트
        */

        //속성 목록 선택 및 로드
        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Attr loaded = dm.GetAttrList()[listBox1.SelectedIndex];

            if (!grid0.IsEnabled)
            {
                grid0.IsEnabled = true;
                buttonXmlWindow.IsEnabled = true;
            }

            textBoxName.Text = loaded.ToString(); //속성 이름 로드
            listBox2.DataContext = null;    //리스트2에 데이터 정렬해서 출력
            listBox2.DataContext = dm.GetCountList(listBox1.SelectedIndex);

            //비식별화 종류 로드
            switch (loaded.type)
            {
                case Attr.attrType.qi:
                    radioButtonType0.IsChecked = true;
                    break;
                case Attr.attrType.sa:
                    radioButtonType1.IsChecked = true;
                    break;
                case Attr.attrType.attr:
                    radioButtonType2.IsChecked = true;
                    break;
            }

            //데이터 타입 종류 로드
            radioButtonDataType0.IsChecked = !loaded.numerical;
            radioButtonDataType1.IsChecked = loaded.numerical;

            //범위 편집 텍스트 변경
            textBlock1.Text = loaded.numerical ? "숫자 범위 편집 (Numeric tree 편집)" : "문자 범위 편집 (Categorical tree 편집)";

            //XML 파일 주소 로드
            textBoxPath.Text = loaded.path;
           
        }

        //속성 이름 변경
        private void textBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(textBoxName.Text != "")      //그 외의 경우는 익셉션
            {
                dm.UpdateAttrName(listBox1.SelectedIndex, textBoxName.Text);
                listBox1.Items.Refresh();
            }
        }

        //비식별화 종류 변경
        private void radioButtonType_Checked(object sender, RoutedEventArgs e)
        {
            string tag = (sender as RadioButton).Tag.ToString();

            switch (tag)
            {
                case "qi":
                    dm.UpdateAttrType(listBox1.SelectedIndex, Attr.attrType.qi);
                    break;
                case "sa":
                    dm.UpdateAttrType(listBox1.SelectedIndex, Attr.attrType.sa);
                    break;
                case "attr":
                    dm.UpdateAttrType(listBox1.SelectedIndex, Attr.attrType.attr);
                    break;
            }

            buttonXmlWindow.IsEnabled = !(tag == "attr");
        }

        //데이터 타입 종류 변경
        private void radioButtonDataType_Checked(object sender, RoutedEventArgs e)
        {
            dm.UpdateAttrNumeric(listBox1.SelectedIndex, (bool)(radioButtonDataType1.IsChecked));
            listBox2.DataContext = null;    //리스트2에 데이터 정렬해서 출력
            listBox2.DataContext = dm.GetCountList(listBox1.SelectedIndex);

            //범위 편집 텍스트 변경
            textBlock1.Text = (bool)(radioButtonDataType1.IsChecked) ? "숫자 범위 편집 (Numeric tree 편집)" : "문자 범위 편집 (Categorical tree 편집)";
        }

        //k,l,t 변경
        private void IntValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"\d");
            e.Handled = !regex.IsMatch(e.Text);
        }

        //k, l 값 처리. l값은 k 값 이하여야 한다.
        private void KL_LostFocus(object sender, RoutedEventArgs e)
        {
            int k, l;

            k = int.TryParse(textBoxK.Text, out k) ? k : 0;
            l = int.TryParse(textBoxL.Text, out l) ? l : 0;

            if(k < l)
            {
                MessageBox.Show("K 값은 L 값보다 크거나 같아야 합니다.");
                textBoxL.Text = k + "";
            }

        }


        //파일 체크박스 변경
        private void checkbox0_Checked(object sender, RoutedEventArgs e)
        {
            textBoxOutputFile.IsEnabled = !(bool) (checkbox0.IsChecked);
            textBoxOutputFile.Text = textBoxInputFile.Text + "_output.txt";
            textBoxLogFile.IsEnabled = !(bool) (checkbox1.IsChecked);
            textBoxLogFile.Text = textBoxInputFile.Text + "_log.txt";
        }

        //편집 버튼 클릭시, 트리 편집 창 생성
        private void buttonXmlWindow_Click(object sender, RoutedEventArgs e)
        {
            //Numeric의 경우
            if ((bool) radioButtonDataType1.IsChecked)
            {
                XmlWindow xmlWin = new XmlWindow(listBox1.SelectedIndex);
                xmlWin.Owner = this;
                xmlWin.ShowDialog();
                textBoxPath.Text = (dm.GetAttrList()[listBox1.SelectedIndex] as Attr).path;

            }

            //Categorical의 경우
            else
            {
                XmlCategoricalWindow xmlCWin = new XmlCategoricalWindow(listBox1.SelectedIndex);
                xmlCWin.Owner = this;
                xmlCWin.ShowDialog();
                textBoxPath.Text = (dm.GetAttrList()[listBox1.SelectedIndex] as Attr).path;
            }

        }


        /*
            데이터 탭 파트
        */

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tabName = ((sender as TabControl).SelectedValue as TabItem).Name;

            switch (tabName)
            {
                case "tabTable":
                    if(e.Source is TabControl)
                    {
                        dataGrid1.DataContext = null;       //초기화
                        dataGrid1.DataContext = dm.GetDataTable().DefaultView;      //데이터를 테이블에 로드

                    }
                    break;
            }
        }
    }
}
