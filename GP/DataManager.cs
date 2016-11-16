using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Controls;

namespace GP
{
    class DataManager
    {
        private static DataManager INSTANCE;
        
        private int numberOfAttr;   //속성의 개수
        private readonly DataTable db;       //데이터를 저장하는 테이블
        private readonly List<Attr> attrList;    //속성을 저장하는 리스트
        private string presetPath = "";      //프리셋 파일 주소


        private DataManager()
        {
            numberOfAttr = 0;
            db = new DataTable();
            attrList = new List<Attr>();
        }
       
        public bool OpenFile(string path)
        {
            char[] delimiters = { ',' };
            string line;
            
            using (StreamReader sr = new StreamReader(path))
            {
                if((line = sr.ReadLine()) == null)
                {
                    //Error! 빈 파일
                    PrintError("Error: Empty file");
                    return false;
                }

                //먼저 첫 줄을 읽는다.
                string[] firstPart = line.Split(delimiters);
                numberOfAttr = firstPart.Length;    //속성의 개수를 파악
                foreach (int i in Enumerable.Range(1, numberOfAttr))
                {
                    db.Columns.Add(i + "열");
                    attrList.Add(new Attr(i + "열"));    //속성들을 속성 리스트에 삽입
                }
                db.Rows.Add().ItemArray = firstPart;    //데이터를 테이블에 삽입

                //나머지를 읽는다.
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split(delimiters);

                    if (numberOfAttr != parts.Length)
                    {
                        //Error! csv 파일에서 속성의 개수가 이전과 다를 경우
                        PrintError("Error: Wrong data");
                        return false;
                    }
                    numberOfAttr = parts.Length;    //속성의 개수를 기록
                    db.Rows.Add().ItemArray = parts;    //데이터를 테이블에 삽입
                }
            }
            return true;
        }
        

        public void SaveConfigXML(string path, Config cfg)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("\t");

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteComment(" Anonymization Configuration File.");

                //시작
                writer.WriteStartElement("configuration");

                //setting 기록
                writer.WriteStartElement("setting");
                writer.WriteAttributeString("k", cfg.k.ToString());
                writer.WriteAttributeString("l", cfg.l.ToString());
                writer.WriteAttributeString("t", cfg.t.ToString());
                writer.WriteAttributeString("algo", "incognito");
                writer.WriteEndElement();

                //input 기록
                writer.WriteStartElement("input");
                writer.WriteAttributeString("filename", cfg.input);
                writer.WriteEndElement();

                //output 기록
                writer.WriteStartElement("output");
                writer.WriteAttributeString("filename", cfg.output);
                writer.WriteEndElement();

                //log 기록
                writer.WriteStartElement("log");
                writer.WriteAttributeString("filename", cfg.log);
                writer.WriteEndElement();

                //column 시작
                writeColumn(writer);

                //끝
                writer.WriteEndElement();   //configuration 끝
                
                writer.WriteEndDocument();
                writer.Close();
            }
        }

        private void writeColumn(XmlWriter writer)
        {
            //column 시작
            writer.WriteStartElement("column");

            for (int i = 0; i < attrList.Count; i++)
            {
                Attr a = attrList[i];
                writer.WriteStartElement("attr");
                writer.WriteAttributeString("name", a.name);
                writer.WriteAttributeString("index", i.ToString());
                writer.WriteAttributeString("type", a.type.ToString());

                if(a.type != Attr.attrType.attr)
                {
                    string pathName = (a.path == "") ? "" : @"./XML/" + a.path;
                    writer.WriteAttributeString("path", pathName);
                }
                writer.WriteEndElement();
            }
            
            //column 끝
            writer.WriteEndElement();

        }

        public void writeTaxonomy(string path, bool numerical, ItemCollection items, List<Entry> list = null)
        {
            if (path == "")
            {
                path = "default.xml";
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("\t");

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteComment(" Attribute Taxnomy Tree Configuration File.");

                //시작
                writer.WriteStartElement("taxonomy");

                if (numerical)
                {
                    //property 기록
                    writer.WriteStartElement("property");
                    writer.WriteAttributeString("type", "numerical");
                    writer.WriteEndElement();

                    //tree 시작
                    writer.WriteStartElement("tree");

                    writeNumericTree(writer, items);

                    //tree 끝
                    writer.WriteEndElement();

                }
                else
                {
                    //property 기록
                    writer.WriteStartElement("property");
                    writer.WriteAttributeString("type", "categorical");
                    writer.WriteEndElement();

                    //map 시작
                    writer.WriteStartElement("map");

                    foreach(Entry e in list)
                    {
                        writer.WriteStartElement("entry");
                        writer.WriteAttributeString("value", e.index + "");
                        writer.WriteAttributeString("name", e.key);

                        writer.WriteEndElement();
                    }

                    //map 끝
                    writer.WriteEndElement();

                    //tree 시작
                    writer.WriteStartElement("tree");

                    writeCategoricalTree(writer, items);

                    //tree 끝
                    writer.WriteEndElement();
                }

                //끝
                writer.WriteEndElement();   //taxonomy 끝

                writer.WriteEndDocument();
                writer.Close();
            }
        }

        private void writeNumericTree(XmlWriter writer, ItemCollection items)
        {
            foreach(XmlWindow.NumericTreeViewItem c in items)
            {
                writer.WriteStartElement("node");
                writer.WriteAttributeString("range", c.range);

                if (c.Items.Count > 0)
                    writeNumericTree(writer, c.Items);
                writer.WriteEndElement();
            }
        }

        private void writeCategoricalTree(XmlWriter writer, ItemCollection items)
        {
            foreach(XmlCategoricalWindow.CategoricalTreeViewItem c in items)
            {
                writer.WriteStartElement("node");
                writer.WriteAttributeString("name", c.nameAttribute);
                writer.WriteAttributeString("range", c.range);

                if (c.Items.Count > 0)
                    writeCategoricalTree(writer, c.Items);
                writer.WriteEndElement();
            }
        }

        public void SavePresetPath(string path)
        {
            presetPath = path;
        }

        private void PrintError(string error)
        {
            Console.WriteLine(error);
        }


        /*
            Getter
        */

        public static DataManager GetDataManager()
        {
            if (INSTANCE == null)
                INSTANCE = new DataManager();
            return INSTANCE;
        }

        public DataTable GetDataTable()
        {
            return db;
        }
        
        public List<Attr> GetAttrList()
        {
            return attrList;
        }

        public List<Entry> GetCountList(int index)
        {
            if (attrList[index].numerical)
            {
                List<float> tmpList = GetNumericList(index);
                List<Entry> result = tmpList
                    .GroupBy(g => g)
                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                    .Select(group => new Entry(group.Key.ToString(), group.Count(), 0, true))
                    .ToList();

                return result;
            }

            else
            {
                return GetCategoricalList(index, true);
            }
        }

        public List<float> GetNumericList(int index)
        {
            List<string> extractedList = db.AsEnumerable()
                .Select(x => x[index].ToString()).ToList();

            float output;

            return extractedList.Select(o => float.TryParse(o, out output) ? output : float.MaxValue).ToList().OrderBy(o => o).ToList();
        }

        public List<Entry> GetCategoricalList(int index, bool hideIndex = false)
        {
            List<string> extractedList = db.AsEnumerable()
                .Select(x => x[index].ToString()).ToList();

            List<Entry> entryList = extractedList
                .GroupBy(g => g)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                .Select(group => new Entry(group.Key, group.Count(), 0, hideIndex))
                .ToList();

            return entryList;
        }

        public string GetPresetPath()
        {
            return presetPath;
        }

        /*
            속성 업데이트
        */
        public void UpdateAttrName(int index, string name)
        {
            string tmp = db.Columns.Contains(name) ? "_" : "";      //중복 시 에러를 막기 위해 뒤에 _ 붙임

            db.Columns[index].ColumnName = name + tmp;
            attrList[index].name = name;
        }

        public void UpdateAttrType(int index, Attr.attrType type)
        {
            attrList[index].type = type;
        }

        public void UpdateAttrNumeric(int index, bool isNumeric)
        {
            attrList[index].numerical = isNumeric;
        }
        
    }
}
