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
using System.Xml;

namespace GP
{
    public partial class XmlWindow : Window
    {
        private readonly DataManager dm;
        private static List<float> list;
        private readonly int index;

        public class NumericTreeViewItem : TreeViewItem
        {
            public float min;       //범위 최소
            public float max;       //범위 최대
            public bool includeMin;     //최소값을 포함
            public bool includeMax;     //최대값을 포함
            public int count;       //범위 내의 데이터 개수
            public readonly NumericTreeViewItem owner;        //부모노드
            public string range;    //xml 기록용

            

            public NumericTreeViewItem(float min = 0, float max = 0, NumericTreeViewItem owner = null)
            {
                UpdateInfo(min, max, true, true);
                IsSelected = true;
                this.owner = owner;
            }

            public void UpdateInfo(float min, float max, bool includeMin, bool includeMax)
            {
                //값 갱신
                this.min = min;
                this.max = max;
                this.includeMin = includeMin;
                this.includeMax = includeMax;

                //범위 내 데이터 수 갱신
                count = (includeMin) ? ((includeMax)? list.Count(c => c >= min && c <= max) : list.Count(c => c >= min && c < max))
                    : ((includeMax) ? list.Count(c => c > min && c <= max) : list.Count(c => c > min && c < max));


                //헤더 갱신
                string head = includeMin ? "[" : "(";
                string tail = includeMax ? "]" : ")";

                range = head + min + "-" + max + tail;        //xml에 기록될 range attribute

                Header = range + " (#" + count + "개)";       //treeview에 노출
            }
        }

        public XmlWindow(int selectedIndex)
        {
            InitializeComponent();

            dm = DataManager.GetDataManager();
            list = dm.GetNumericList(selectedIndex);
            index = selectedIndex;

            textBlockCount.Text = "데이터 개수 : " + list.Count;
            textBlockMin.Text = "최소값 : " + list.Min();
            textBlockMax.Text = "최대값 : " + list.Max();

            textBoxPath.Text = (dm.GetAttrList()[index] as Attr).path;

            LoadItem();
        }

        //아이템 로드
        private void LoadItem()
        {
            if (textBoxPath.Text == "")
            {
                treeView.Items.Add(new NumericTreeViewItem(list.Min(), list.Max(), null));
                textBoxPath.Text = "defalut" + index + ".xml";
                Save();
            }

            else
            {
                bool isOkay = false;
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + @"XML\" + textBoxPath.Text);

                XmlNode root = doc.FirstChild.NextSibling.NextSibling;      //xml과 comment를 생략. taxonomy 노드

                foreach (XmlNode node in root)
                {
                    if(node.NodeType == XmlNodeType.Element)
                    {
                        switch (node.Name)
                        {
                            case "property":
                                isOkay = node.Attributes["type"].Value == "numerical";
                                if (!isOkay)
                                {
                                    MessageBox.Show("데이터 종류가 다릅니다. 기존의 트리가 삭제됩니다.", "데이터 종류 변경 경고");
                                }
                                break;
                            case "tree":
                                if (isOkay)
                                {
                                    LoadNode(node, treeView.Items);
                                }
                                break;
                        }
                    }
                }

                if (!isOkay)
                {
                    treeView.Items.Add(new NumericTreeViewItem(list.Min(), list.Max(), null));
                    textBoxPath.Text = "defalut" + index + ".xml";
                    Save();
                }
            }
        }
        
        private void LoadNode(XmlNode parent, ItemCollection items)
        {
            foreach(XmlNode child in parent)
            {
                if(child.NodeType == XmlNodeType.Element && child.Name == "node")
                {
                    string tmp = child.Attributes["range"].Value;

                    bool withMin = tmp.Substring(0, 1) == "[";
                    bool withMax = tmp.Substring(tmp.Length - 1, 1) == "]";

                    string tmp2 = tmp.Substring(1, tmp.Length - 2);
                    int tmpIndex = tmp2.IndexOf('-');

                    float min, max;
                    float.TryParse(tmp2.Substring(0, tmpIndex), out min);
                    float.TryParse(tmp2.Substring(tmpIndex + 1), out max);

                    NumericTreeViewItem c = new NumericTreeViewItem();
                    c.UpdateInfo(min, max, withMin, withMax);

                    items.Add(c);

                    if (child.HasChildNodes)
                    {
                        LoadNode(child, c.Items);
                    }
                }
            }
            
        }

        //프리셋 로드
        private void LoadPreset(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);     //이 path는 ..\Preset\파일명.xml

            XmlNode root = doc.FirstChild.NextSibling.NextSibling;      //xml과 comment를 생략. taxonomy 노드
            

            foreach (XmlNode node in root)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    switch (node.Name)
                    {
                        case "tree":
                            LoadNode(node, treeView.Items);
                            break;
                    }
                }
            }
            
            //treeview 초기화
            treeView.Items.RemoveAt(0);
        }



        //노드 선택
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //노드의 min, max 로드
            textBoxMin.Text = (treeView.SelectedItem as NumericTreeViewItem).min.ToString();
            textBoxMax.Text = (treeView.SelectedItem as NumericTreeViewItem).max.ToString();

            //포함여부 로드
            checkBoxMin.IsChecked = (treeView.SelectedItem as NumericTreeViewItem).includeMin;
            checkBoxMax.IsChecked = (treeView.SelectedItem as NumericTreeViewItem).includeMax;


            //루트 노드의 경우 노드 삭제 버튼 비활성화
            buttonDelete.IsEnabled = (treeView.SelectedItem as NumericTreeViewItem).owner != null;                
        }


        //하위 노드 생성
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            NumericTreeViewItem parent = (treeView.SelectedItem as NumericTreeViewItem);

            parent.Items.Add(new NumericTreeViewItem(parent.min, parent.max, parent) { IsExpanded = true });
            parent.IsExpanded = true;   //확장
        }


        //선택 노드 삭제
        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((treeView.SelectedItem as NumericTreeViewItem).Items.Count == 0)
            {
                NumericTreeViewItem parent = (treeView.SelectedItem as NumericTreeViewItem).owner;

                parent.Items.Remove(treeView.SelectedItem);
                parent.IsSelected = true;  //삭제 후에 부모 노드 선택
            }

            else
            {
                MessageBox.Show("먼저 모든 하위노드를 삭제 해야합니다.");
            }
        }

        //범위값 체크
        private bool checkRange(NumericTreeViewItem item)
        {

        }

        //min, max 값 적용; todo: 하위노드 비교
        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            float min, max;
            NumericTreeViewItem parent = (treeView.SelectedItem as NumericTreeViewItem).owner;

            if(float.TryParse(textBoxMin.Text, out min) && float.TryParse(textBoxMax.Text, out max))
            {
                if(min > max)
                {
                    MessageBox.Show("최소값이 최대값보다 큽니다.");
                }

                else if((parent != null) && (min < parent.min || max > parent.max))
                {
                    MessageBox.Show("상위 노드의 범위를 벗어납니다.");
                }

                else
                {
                    //적용
                    (treeView.SelectedItem as NumericTreeViewItem).UpdateInfo(min, max, (bool) checkBoxMin.IsChecked, (bool) checkBoxMax.IsChecked);
                }
            }
            else
            {
                MessageBox.Show("잘못된 값을 입력하셨습니다.");
            }

        }

        //자동 버튼 클릭
        private void buttonAuto_Click(object sender, RoutedEventArgs e)
        {
            textBoxPath.Text = dm.GetAttrList()[index].name + ".xml";
        }

        //프리셋 버튼 클릭
        private void buttonPreset_Click(object sender, RoutedEventArgs e)
        {
            Preset presetDialog = new Preset();

            //리스트 다이얼로그에서 프리셋 리턴 받음
            presetDialog.ShowDialog();
            
            //다이얼로그 종료 후 프리셋 로드
            if(dm.GetPresetPath() != "")
            {
                LoadPreset(dm.GetPresetPath());
            }
        }

        //종료 시에 자동 저장
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"XML\");
            dm.writeTaxonomy(AppDomain.CurrentDomain.BaseDirectory + @"XML\"  + textBoxPath.Text, true, treeView.Items);
            (dm.GetAttrList()[index] as Attr).path = textBoxPath.Text;
        }

    }
}
