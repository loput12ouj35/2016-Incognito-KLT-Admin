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
    public partial class XmlCategoricalWindow : Window
    {
        private readonly DataManager dm;
        private static List<Entry> listC;
        private readonly int countOfdata;
        private readonly int index;
        private bool hasErrorRange;
        

        public class CategoricalTreeViewItem : TreeViewItem
        {
            public int min;
            public int max;
            public int count;       //범위 내의 데이터 개수
            public readonly CategoricalTreeViewItem owner;        //부모노드
            public string range;    //xml 기록용, 범위 attribute
            public string nameAttribute;     //xml 기록용, 이름 attribute

            public CategoricalTreeViewItem(int min = 0, int max = 0, CategoricalTreeViewItem owner = null, string name ="새로운 노드")
            {
                UpdateInfo(min, max, name);
                IsSelected = true;
                this.owner = owner;
            }

            public void UpdateInfo(int min, int max, string name)
            {
                //값 갱신
                this.min = min;
                this.max = max;

                //범위 내 데이터 수 갱신
                count = 0;
                for(int i = min; i <= max; i++)
                {
                    Entry tmp = listC[i];
                    count += tmp.count;
                }


                //헤더 갱신
                const string head = "[";
                const string tail = "]";

                range = head + min + "-" + max + tail;        //xml에 기록될 range attribute
                nameAttribute = name;       //이름 갱신 + xml에 기록될 name attribute

                Header = nameAttribute + " " + range + " (#" + count + "개)";       //treeview에 노출
            }

            public void UpdateInfo()
            {
                //범위 내 데이터 수 갱신
                count = 0;
                for (int i = min; i <= max; i++)
                {
                    Entry tmp = listC[i];
                    count += tmp.count;
                }

                //헤더 갱신
                const string head = "[";
                const string tail = "]";

                range = head + min + "-" + max + tail;        //xml에 기록될 range attribute

                Header = nameAttribute + " " + range + " (#" + count + "개)";       //treeview에 노출
            }
        }

        public XmlCategoricalWindow(int selectedIndex)
        {
            InitializeComponent();

            dm = DataManager.GetDataManager();
            listC = dm.GetCategoricalList(selectedIndex);
            UpdateEntryList();
            index = selectedIndex;
            hasErrorRange = false;

            int dataCount = 0;
            foreach(Entry tmp in listC)
            {
                dataCount += tmp.count;
            }

            countOfdata = dataCount;
            textBlockCountC.Text = "데이터 개수 : " + dataCount;
            textBlockEntryNumberC.Text = "엔트리 개수 : " + listC.Count;

            textBoxPathC.Text = (dm.GetAttrList()[index] as Attr).path;

            LoadItem();
        }

        //아이템 로드
        private void LoadItem()
        {
            if (textBoxPathC.Text == "")
            {
                treeViewC.Items.Add(new CategoricalTreeViewItem(0, listC.Count - 1, null, "전체 데이터 범위"));
                textBoxPathC.Text = "defalut" + index + ".xml";
                Save();
            }

            else
            {
                bool isOkay = false;
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + @"XML\" + textBoxPathC.Text);

                XmlNode root = doc.FirstChild.NextSibling.NextSibling;      //xml과 comment를 생략. taxonomy 노드

                foreach (XmlNode node in root)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        switch (node.Name)
                        {
                            case "property":
                                isOkay = node.Attributes["type"].Value == "categorical";
                                if (!isOkay)
                                {
                                    MessageBox.Show("데이터 종류가 다릅니다. 기존의 트리가 삭제됩니다.", "데이터 종류 변경 경고");
                                }
                                break;
                            case "map":
                                LoadMap(node);
                                break;
                            case "tree":
                                if (isOkay)
                                {
                                    LoadNode(node, treeViewC.Items, null);
                                }
                                break;
                        }
                    }
                }

                if (!isOkay)
                {
                    treeViewC.Items.Add(new CategoricalTreeViewItem(0, listC.Count - 1, null, "전체 데이터 범위"));
                    textBoxPathC.Text = "defalut" + index + ".xml";
                    Save();
                }
            }

        }

        private void LoadNode(XmlNode parent, ItemCollection items, CategoricalTreeViewItem parentItem)
        {
            foreach (XmlNode child in parent)
            {
                if (child.NodeType == XmlNodeType.Element && child.Name == "node")
                {
                    string tmp = child.Attributes["range"].Value;
                    string name = child.Attributes["name"].Value;

                    string tmp2 = tmp.Substring(1, tmp.Length - 2);
                    int tmpIndex = tmp2.IndexOf('-');

                    int min, max;
                    int.TryParse(tmp2.Substring(0, tmpIndex), out min);
                    int.TryParse(tmp2.Substring(tmpIndex + 1), out max);

                    CategoricalTreeViewItem c = new CategoricalTreeViewItem(0, 0, parentItem);
                    c.UpdateInfo(min, max, name);

                    items.Add(c);

                    if (child.HasChildNodes)
                    {
                        LoadNode(child, c.Items, c);
                    }
                }
            }

        }

        private void LoadMap(XmlNode parent)
        {
            List<Entry> loadedList = new List<Entry>();

            foreach(XmlNode child in parent)
            {
                if (child.NodeType == XmlNodeType.Element && child.Name == "entry")
                {
                    string name = child.Attributes["name"].Value;

                    loadedList.Add(new Entry(name, 0));
                    
                }

            }


            //카운트 로드 및 인덱스 설정
            int tmp = 0;
            foreach(Entry loadedEntry in loadedList)
            {
                loadedEntry.index = tmp;
                foreach(Entry originalEntry in listC)
                {
                    if(loadedEntry.key == originalEntry.key)
                    {
                        loadedEntry.count = originalEntry.count;
                    }
                }
                tmp++;
            }


            listC = loadedList;

            //갱신
            UpdateEntryList();
        }



        //노드 선택
        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //노드의 min, max 로드
            textBoxMinC.Text = (treeViewC.SelectedItem as CategoricalTreeViewItem).min.ToString();
            textBoxMaxC.Text = (treeViewC.SelectedItem as CategoricalTreeViewItem).max.ToString();

            //노드의 이름 로드
            textBoxNameC.Text = (treeViewC.SelectedItem as CategoricalTreeViewItem).nameAttribute;

            //루트 노드의 경우 노드 삭제 버튼 비활성화
            buttonDeleteC.IsEnabled = (treeViewC.SelectedItem as CategoricalTreeViewItem).owner != null;
        }


        //하위 노드 생성
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            CategoricalTreeViewItem parent = (treeViewC.SelectedItem as CategoricalTreeViewItem);

            parent.Items.Add(new CategoricalTreeViewItem(parent.min, parent.max, parent, "새로운 범위") { IsExpanded = true });
            parent.IsExpanded = true;   //확장
            checkRange();       //확장 후에 범위 에러 체크
        }


        //선택 노드 삭제
        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((treeViewC.SelectedItem as CategoricalTreeViewItem).Items.Count == 0)
            {
                CategoricalTreeViewItem parent = (treeViewC.SelectedItem as CategoricalTreeViewItem).owner;

                parent.Items.Remove(treeViewC.SelectedItem);
                parent.IsSelected = true;  //삭제 후에 부모 노드 선택
                checkRange();           //삭제 후에 범위 에러 체크
            }

            else
            {
                MessageBox.Show("먼저 모든 하위노드를 삭제 해야합니다.");
            }
        }

        private void checkRange()
        {
            hasErrorRange = checkRangeofChild((treeViewC.Items[0] as CategoricalTreeViewItem).Items, (treeViewC.Items[0] as CategoricalTreeViewItem).min, (treeViewC.Items[0] as CategoricalTreeViewItem).max)
                            || checkRangeofRoot();
        }

        //루트의 범위값 체크
        private bool checkRangeofRoot()
        {
            bool tmp = (treeViewC.Items[0] as CategoricalTreeViewItem).count != countOfdata;
            (treeViewC.Items[0] as CategoricalTreeViewItem).Foreground = tmp ? Brushes.Green : Brushes.Black;

            return tmp;
        }

        //자식의 범위값 체크
        private bool checkRangeofChild(ItemCollection item, float min, float max)
        {
            bool isError = false;
            int index = 0;
            float bound = min;

            //빨간색 해지 및 개수 계산
            foreach (CategoricalTreeViewItem child in item)
            {
                child.Foreground = Brushes.Black;
            }


            //에러 체크 시작
            foreach (CategoricalTreeViewItem child in item)
            {
                //처음 부분
                if (index == 0)
                {
                    if (child.min != min)
                    {
                        isError = true;
                        child.Foreground = Brushes.Red;     //처음 값이 최소값이 아님
                    }
                }

                //가운데 부분
                else if (child.min != bound + 1)
                {
                    isError = true;
                    child.Foreground = Brushes.Red;       //바운드를 벗어남, 빠지는 부분이 있음
                }

                //마지막 부분만 추가 체크
                if (index == item.Count - 1 && child.max != max)
                {
                    isError = true;
                    child.Foreground = Brushes.Red;     //마지막 값이 최대값이 아님
                }

                //개수가 0개라면 의미가 없음
                if (child.count == 0)
                {
                    isError = true;
                    child.Foreground = Brushes.Blue;
                }

                //recursive
                else if (child.Items.Count > 0)
                    checkRangeofChild(child.Items, child.min, child.max);


                bound = child.max;
                index++;
            }

            return isError;
        }

        //min, max 값 적용, 이름 변경
        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            int min, max;
            CategoricalTreeViewItem parent = (treeViewC.SelectedItem as CategoricalTreeViewItem).owner;

            if (int.TryParse(textBoxMinC.Text, out min) && int.TryParse(textBoxMaxC.Text, out max))
            {
                if (min > max)
                {
                    MessageBox.Show("최소값이 최대값보다 큽니다.");
                }

                else if ((parent != null) && (min < parent.min || max > parent.max))
                {
                    MessageBox.Show("상위 노드의 범위를 벗어납니다.");
                }

                else if (min < 0)
                {
                    MessageBox.Show("인덱스 값은 0 이상이어야 합니다.");
                }

                else if (max >= listC.Count)
                {
                    MessageBox.Show("인덱스 값은 엔트리 개수 이하이어야 합니다.");
                }


                else
                {
                    //적용
                    (treeViewC.SelectedItem as CategoricalTreeViewItem).UpdateInfo(min, max, textBoxNameC.Text);
                    checkRange();       //적용 후에 범위 에러 체크
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
            textBoxPathC.Text = dm.GetAttrList()[index].name + ".xml";
        }

        //종료 시에 자동 저장
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            checkRange();       //종료 전 체크
            if (hasErrorRange)
                MessageBox.Show("빠지거나 중복된 범위가 있습니다.", "에러");

            Save();
        }

        private void Save()
        {
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"XML\");
            dm.writeTaxonomy(AppDomain.CurrentDomain.BaseDirectory + @"XML\" + textBoxPathC.Text, false, treeViewC.Items, listC);
            (dm.GetAttrList()[index] as Attr).path = textBoxPathC.Text;
        }

        //리스트 정렬 버튼 클릭
        private void buttonSort_Click(object sender, RoutedEventArgs e)
        {
            int si = listBoxC.SelectedIndex;    //선택된 아이템의 인덱스
            int li = si; //연산에 선택되는 인덱스

            if(si >= 0)
            {
                string tag = (sender as Button).Tag.ToString();
                Entry tmp = listC[si];

                switch (tag)
                {
                    case "top":
                        li = 0;
                        listC.RemoveAt(si);
                        listC.Insert(li, tmp);
                        break;
                    case "up":
                        li = si - 1;
                        if (li >= 0)
                        {
                            listC[si] = listC[li];
                            listC[li] = tmp;
                        }
                        break;
                    case "down":
                        li = si + 1;
                        if (li < listC.Count)
                        {
                            listC[si] = listC[li];
                            listC[li] = tmp;
                        }
                        break;
                    case "bot":
                        li = listC.Count - 1;
                        listC.RemoveAt(si);
                        listC.Add(tmp);
                        break;
                }

                //리스트 박스 갱신 파트
                UpdateEntryList();
                listBoxC.SelectedIndex = li;
                

                //트리 갱신 파트
                UpdateAllItems(treeViewC.Items);
            }
            
        }

        //트리뷰 새로고침
        private void UpdateAllItems(ItemCollection items)
        {
            foreach(CategoricalTreeViewItem c in items)
            {
                c.UpdateInfo();

                if(c.Items.Count > 0)
                {
                    UpdateAllItems(c.Items);
                }
            }
        }

        //엔트리 리스트 새로고침
        private void UpdateEntryList()
        {
            for(int i = 0; i < listC.Count; i++)
            {
                listC[i].index = i;
            }

            listBoxC.DataContext = null;
            listBoxC.DataContext = listC;
        }
    }
}
