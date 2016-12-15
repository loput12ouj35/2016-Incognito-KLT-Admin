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
        private bool hasErrorRange;

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
            hasErrorRange = false;

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
                                    LoadNode(node, treeView.Items, null);
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
        
        private void LoadNode(XmlNode parent, ItemCollection items, NumericTreeViewItem parentItem)
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

                    NumericTreeViewItem c = new NumericTreeViewItem(0, 0, parentItem);
                    c.UpdateInfo(min, max, withMin, withMax);

                    items.Add(c);

                    if (child.HasChildNodes)
                    {
                        LoadNode(child, c.Items, c);
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
                            LoadNode(node, treeView.Items, null);
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
            checkRange();       //확장 후에 범위 에러 체크
        }


        //선택 노드 삭제
        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((treeView.SelectedItem as NumericTreeViewItem).Items.Count == 0)
            {
                NumericTreeViewItem parent = (treeView.SelectedItem as NumericTreeViewItem).owner;

                parent.Items.Remove(treeView.SelectedItem);
                parent.IsSelected = true;  //삭제 후에 부모 노드 선택
                checkRange();       //삭제 후에 범위 에러 체크
            }

            else
            {
                MessageBox.Show("먼저 모든 하위노드를 삭제 해야합니다.");
            }
        }

        //전체 트리 범위값 체크
        private void checkRange()
        {
            hasErrorRange = checkRangeofChild((treeView.Items[0] as NumericTreeViewItem).Items, (treeView.Items[0] as NumericTreeViewItem).min, (treeView.Items[0] as NumericTreeViewItem).max)
                            || checkRangeofRoot();
        }

        //루트의 범위값 체크
        private bool checkRangeofRoot()
        {
            bool tmp = (treeView.Items[0] as NumericTreeViewItem).count != list.Count;
            (treeView.Items[0] as NumericTreeViewItem).Foreground = tmp? Brushes.Green : Brushes.Black;

            return tmp;
        }

        //자식의 범위값 체크;
        private bool checkRangeofChild(ItemCollection item, float min, float max)
        {
            bool isError = false;       //결과
            int index = 0;      //인덱스
            float bound = min;      //경계 값을 저장
            bool includeBound = false;  //경계 값을 포함하는 여부를 저장

            //빨간색 해지 및 개수 계산
            foreach(NumericTreeViewItem child in item)
            {
                child.Foreground = Brushes.Black;
            }


            //에러 체크 시작
            foreach(NumericTreeViewItem child in item)
            {
                //처음 부분
                if (index == 0)
                {
                    if (child.min != min)
                    {
                        isError = true;
                        child.Foreground = Brushes.Red;     //처음 값이 최소값이 아님
                    }
                    else if ((child.Parent != null) && (child.includeMin != (child.Parent as NumericTreeViewItem).includeMin))
                    {
                        isError = true;
                        child.Foreground = Brushes.Red;     //최소값의 포함 여부가 부모와 다름
                    }
                }

                //가운데 부분과 마지막 부분
                else if ((child.min != bound) || (child.includeMin == includeBound))
                {
                    isError = true;
                    child.Foreground = Brushes.Red;       //바운드를 벗어남, 빠지는 부분이 있음
                }

                //마지막 부분만 추가 체크
                if (index == item.Count - 1)
                {
                    if (child.max != max)
                    {
                        isError = true;
                        child.Foreground = Brushes.Red;     //마지막 값이 최대값이 아님
                    }
                    else if ((child.Parent != null) && (child.includeMax != (child.Parent as NumericTreeViewItem).includeMax))
                    {
                        isError = true;
                        child.Foreground = Brushes.Red;     //최대값 포함 여부가 부모와 다름
                    }
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
                includeBound = child.includeMax;
                index++;
            }

            return isError;
        }

        //min, max 값 적용
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
                    checkRange();   //적용 후에 범위 에러 체크
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
            AutoGenerator autoGeneratorDialog = new AutoGenerator();
            
            autoGeneratorDialog.Owner = this;
            autoGeneratorDialog.Show();
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
            if (hasErrorRange)
                MessageBox.Show("빠지거나 중복된 범위가 있습니다.", "에러");

            Save();
        }

        private void Save()
        {
            System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"XML\");
            dm.writeTaxonomy(AppDomain.CurrentDomain.BaseDirectory + @"XML\"  + textBoxPath.Text, true, treeView.Items);
            (dm.GetAttrList()[index] as Attr).path = textBoxPath.Text;
        }

        //자동화 초기화 
        public void autoMakeTree(AutoGenerator.AutoType autoType, int autoDepth, bool rootRounding = true)
        {
            textBoxPath.Text = dm.GetAttrList()[index].name + ".xml";       //이름 변경

            //treeview 초기화
            treeView.Items.Add(new NumericTreeViewItem(list.Min(), list.Max(), null));
            treeView.Items.RemoveAt(0);

            //root 라운딩
            if (rootRounding)
            {
                roundRoot();
            }

            switch (autoType)
            {
                case AutoGenerator.AutoType.dyn:
                    dynamicMethod(autoDepth);
                    break;
                case AutoGenerator.AutoType.stt:
                    staticMethod(autoDepth);
                    break;
                case AutoGenerator.AutoType.hyb:
                    hybridMethod(autoDepth);
                    break;
                case AutoGenerator.AutoType.heu:
                    heuristicMethod(autoDepth);
                    break;
            }

        }
        
        //루트 라운딩
        private void roundRoot()
        {
            bool minIsZero = list.Min() == 0;
            bool maxIsZero = list.Max() == 0;

            int minDigit = minIsZero ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(list.Min()))) + 1;        //최소값의 자리수
            int maxDigit = maxIsZero ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(list.Max()))) + 1;        //최대값의 자리수


            NumericTreeViewItem root = treeView.Items[0] as NumericTreeViewItem;

            float tmpRootMin = minIsZero ? 0 : (float)Math.Pow(10, minDigit - 1);
            int tmpMaxcoef = (int)(Math.Abs(list.Max()) / Math.Pow(10, maxDigit - 1)) + 1;
            float tmpRootMax = maxIsZero ? 0 : (float) (tmpMaxcoef * Math.Pow(10, maxDigit - 1));

            root.UpdateInfo(tmpRootMin, tmpRootMax, true, false);   //루트노드 업데이트
        
            //노드의 min, max 로드
            textBoxMin.Text = root.min.ToString();
            textBoxMax.Text = root.max.ToString();
        }

        //데이터 분포에 따른 절반 나누기 방법
        private void dynamicMethod(int height)
        {
            dynamicAdd(height, treeView.Items[0] as NumericTreeViewItem, 0, list.Count - 1);
            checkRange();
        }

        //데이터 분포에 따른 절반 나누기 recursive
        private void dynamicAdd(int height, NumericTreeViewItem root, int firstIndex, int lastIndex)
        {
            root.Items.Add(new NumericTreeViewItem(0, 0, root) { IsExpanded = true });
            root.Items.Add(new NumericTreeViewItem(0, 0, root) { IsExpanded = true });

            int midIndex = (firstIndex + lastIndex) / 2;        //중간 인덱스

            //최소와 중간이 같은 경우, 중간 인덱스를 뒤로 옮긴다.
            if (root.min == list[midIndex])
            {
                for(int i = midIndex; i < lastIndex; ++i)
                {
                    if (list[i] > list[midIndex])
                    {
                        midIndex = i;
                        break;
                    }
                }
            }

            //최대와 중간이 같은 경우, 중간 인덱스를 뒤로 옮긴다.
            else if (root.max == list[midIndex])
            {
                for (int i = midIndex; i > firstIndex; --i)
                {
                    if (list[i] < list[midIndex])
                    {
                        midIndex = i;
                        break;
                    }
                }
            }

            (root.Items[0] as NumericTreeViewItem).UpdateInfo(root.min, list[midIndex], root.includeMin, false);
            (root.Items[1] as NumericTreeViewItem).UpdateInfo(list[midIndex], root.max, true, root.includeMax);
            
            root.IsExpanded = true;   //확장


            //준식별자의 경우: 범위 내 값이 k개 미만인 값이 존재한다면 하위 노드 필요 없음
            bool caseA = (dm.GetAttrList()[index] as Attr).type == Attr.attrType.qi && (root.Items[0] as NumericTreeViewItem).count < dm.k;
            bool caseB = (dm.GetAttrList()[index] as Attr).type == Attr.attrType.qi && (root.Items[1] as NumericTreeViewItem).count < dm.k;
            

            //범위 내 값이 0개인 값이 존재한다면 하위 노드 필요 없음
            bool caseC = (root.Items[0] as NumericTreeViewItem).count == 0;
            bool caseD = (root.Items[1] as NumericTreeViewItem).count == 0;
            

            if (caseA || caseB || caseC || caseD)
            {
                root.Items.RemoveAt(1);
                root.Items.RemoveAt(0);
            }

            else if(height > 0)
            {
                dynamicAdd(height - 1, root.Items[0] as NumericTreeViewItem, firstIndex, midIndex);
                dynamicAdd(height - 1, root.Items[1] as NumericTreeViewItem, midIndex, lastIndex);
            }
            
        }
        
        //데이터 값에 따른 절반 나누기 방법
        private void staticMethod(int height)
        {
            staticAdd(height, treeView.Items[0] as NumericTreeViewItem, false);
            checkRange();
        }

        //데이터 값에 따른 절반 나누기 + 데이터 개수 고려 방법
        private void hybridMethod(int height)
        {
            staticAdd(height, treeView.Items[0] as NumericTreeViewItem, true);
            checkRange();
        }

        //데이터 값에 따른 절반 나누기 recursive
        private void staticAdd(int height, NumericTreeViewItem root, bool isHybrid, bool isFloat = false)
        {
            float midValue = isFloat ? (root.min + root.max) / 2 : (int) ((root.min + root.max) / 2);
            
            if (root.min != midValue)
            {
                root.Items.Add(new NumericTreeViewItem(0, 0, root) { IsExpanded = true });
                root.Items.Add(new NumericTreeViewItem(0, 0, root) { IsExpanded = true });

                (root.Items[0] as NumericTreeViewItem).UpdateInfo(root.min, midValue, root.includeMin, false);
                (root.Items[1] as NumericTreeViewItem).UpdateInfo(midValue, root.max, true, root.includeMax);

                root.IsExpanded = true;   //확장

                bool hasChild = true;       //hybrid method에서, 자식이 있는지 삭제되었는지

                if (isHybrid)
                {
                    //준식별자의 경우: 범위 내 값이 k개 미만인 값이 존재한다면 하위 노드 필요 없음
                    bool caseA = (dm.GetAttrList()[index] as Attr).type == Attr.attrType.qi && (root.Items[0] as NumericTreeViewItem).count < dm.k;
                    bool caseB = (dm.GetAttrList()[index] as Attr).type == Attr.attrType.qi && (root.Items[1] as NumericTreeViewItem).count < dm.k;


                    //범위 내 값이 0개인 값이 존재한다면 하위 노드 필요 없음
                    bool caseC = (root.Items[0] as NumericTreeViewItem).count == 0;
                    bool caseD = (root.Items[1] as NumericTreeViewItem).count == 0;


                    if (caseA || caseB || caseC || caseD)
                    {
                        root.Items.RemoveAt(1);
                        root.Items.RemoveAt(0);
                        hasChild = false;
                    }
                }
                
                if (hasChild && height > 0)
                {
                    staticAdd(height - 1, root.Items[0] as NumericTreeViewItem, isHybrid);
                    staticAdd(height - 1, root.Items[1] as NumericTreeViewItem, isHybrid);
                }

            }
        }

        
        //데이터 값에 따르고 분포를 고려하는 절반 나누기 방법
        private void heuristicMethod(int height)
        {
            heuristicAdd(height, treeView.Items[0] as NumericTreeViewItem);
            checkRange();
        }

        //데이터 값에 따르고 분포를 고려하는 절반 나누기 recursive
        private void heuristicAdd(int height, NumericTreeViewItem root, bool isFloat = false)
        {
            float midValue = isFloat ? (root.min + root.max) / 2 : (int)((root.min + root.max) / 2);

            if (root.min != midValue)
            {
                root.Items.Add(new NumericTreeViewItem(0, 0, root) { IsExpanded = true });
                root.Items.Add(new NumericTreeViewItem(0, 0, root) { IsExpanded = true });


                (root.Items[0] as NumericTreeViewItem).UpdateInfo(root.min, midValue, root.includeMin, false);
                (root.Items[1] as NumericTreeViewItem).UpdateInfo(midValue, root.max, true, root.includeMax);

                //데이터 분포 체크
                float dif = ((float) (root.Items[0] as NumericTreeViewItem).count - (root.Items[1] as NumericTreeViewItem).count) / root.count;

                if (dif > 0.3)
                {
                    int coef = dif > 0.7 ? (dif > 0.8 ? 8 : 5) : (dif > 0.5 ? 4 : 3);

                    midValue = isFloat ? root.min + (root.max - root.min) / coef : (int) (root.min + (root.max - root.min) / coef);
                    (root.Items[0] as NumericTreeViewItem).UpdateInfo(root.min, midValue, root.includeMin, false);
                    (root.Items[1] as NumericTreeViewItem).UpdateInfo(midValue, root.max, true, root.includeMax);
                }

                else if (dif < -0.3)
                {
                    int coef = dif < -0.7 ? (dif < -0.8 ? 8 : 5) : (dif < -0.5 ? 4 : 3);

                    midValue = isFloat ? root.max - (root.max - root.min) / coef : (int) (root.max - (root.max - root.min) / coef);
                    (root.Items[0] as NumericTreeViewItem).UpdateInfo(root.min, midValue, root.includeMin, false);
                    (root.Items[1] as NumericTreeViewItem).UpdateInfo(midValue, root.max, true, root.includeMax);
                }

                root.IsExpanded = true;   //확장

                //준식별자의 경우: 범위 내 값이 k개 미만인 값이 존재한다면 하위 노드 필요 없음
                bool caseA = (dm.GetAttrList()[index] as Attr).type == Attr.attrType.qi && (root.Items[0] as NumericTreeViewItem).count < dm.k;
                bool caseB = (dm.GetAttrList()[index] as Attr).type == Attr.attrType.qi && (root.Items[1] as NumericTreeViewItem).count < dm.k;


                //범위 내 값이 0개인 값이 존재한다면 하위 노드 필요 없음
                bool caseC = (root.Items[0] as NumericTreeViewItem).count == 0;
                bool caseD = (root.Items[1] as NumericTreeViewItem).count == 0;


                if (caseA || caseB || caseC || caseD)
                {
                    root.Items.RemoveAt(1);
                    root.Items.RemoveAt(0);
                }

                else if (height > 0)
                {
                    heuristicAdd(height - 1, root.Items[0] as NumericTreeViewItem);
                    heuristicAdd(height - 1, root.Items[1] as NumericTreeViewItem);
                }
            }
            
        }
    }
}
