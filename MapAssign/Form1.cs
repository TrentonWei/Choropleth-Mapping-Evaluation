using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;

using System.Data.OleDb;
using System.Data.Odbc;
using System.IO;

namespace MapAssign
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        #region Parameters
        ILayer pLayer;
        MapAssign.PuTools.Functions function = new PuTools.Functions();
        MapAssign.PuTools.symbolization Symbol = new PuTools.symbolization();
        string localFilePath, fileNameExt, FilePath;
        DataTable table;//Time series data
        #endregion

        /// <summary>
        /// remove the layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 移除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (axMapControl1.Map.LayerCount > 0)
                {
                    if (pLayer != null)
                    {
                        axMapControl1.Map.DeleteLayer(pLayer);
                    }
                }
            }

            catch
            {
                MessageBox.Show("Fail");
                return;
            }
        }

        /// <summary>
        /// Click to remove the layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void axTOCControl1_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            if (axMapControl1.LayerCount > 0)
            {
                esriTOCControlItem pItem = new esriTOCControlItem();
                //pLayer = new FeatureLayerClass();
                IBasicMap pBasicMap = new MapClass();
                object pOther = new object();
                object pIndex = new object();
                // Returns the item in the TOCControl at the specified coordinates.
                axTOCControl1.HitTest(e.x, e.y, ref pItem, ref pBasicMap, ref pLayer, ref pOther, ref pIndex);
            }

            if (e.button == 2)
            {
                this.contextMenuStrip1.Show(axTOCControl1, e.x, e.y);
            }
        }
     
        /// <summary>
        /// load initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            IMap pMap = axMapControl1.Map;
            for (int i = 0; i < pMap.LayerCount; i++)
            {
                ILayer pLayer = pMap.get_Layer(i);
                IDataset LayerDataset = pLayer as IDataset;

                if (LayerDataset != null)
                {
                    if (LayerDataset.Type == esriDatasetType.esriDTFeatureClass)
                    {
                        string strLayerName = pLayer.Name;
                        IFeatureLayer pFeatureLayer = function.GetLayer(pMap, strLayerName);

                        if (pFeatureLayer.FeatureClass.FeatureType != esriFeatureType.esriFTAnnotation)
                        {
                            if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon || pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                            {
                                this.comboBox1.Items.Add(strLayerName);
                            }
                        }
                    }
                }
            }
            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// OutPut
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            this.comboBox2.Items.Clear();
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = " text files|*.txt";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get file path
                localFilePath = saveFileDialog1.FileName.ToString();

                //Get file name
                fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);

                //Get file path exclude the file name
                FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));
            }

            this.comboBox2.Text = localFilePath;
        }

        /// <summary>
        /// refresh the layers of polygon data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_Click(object sender, EventArgs e)
        {
             this.comboBox1.Items.Clear();
             IMap pMap = axMapControl1.Map;
             for (int i = 0; i < pMap.LayerCount; i++)
             {
                 ILayer pLayer = pMap.get_Layer(i);
                 IDataset LayerDataset = pLayer as IDataset;

                 if (LayerDataset != null)
                 {
                     if (LayerDataset.Type == esriDatasetType.esriDTFeatureClass)
                     {
                         string strLayerName = pLayer.Name;
                         IFeatureLayer pFeatureLayer = function.GetLayer(pMap, strLayerName);

                         if (pFeatureLayer.FeatureClass.FeatureType != esriFeatureType.esriFTAnnotation)
                         {
                             if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon||pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                             {
                                 this.comboBox1.Items.Add(strLayerName);
                             }
                         }
                     }
                 }
             }
             if (this.comboBox1.Items.Count > 0)
             {
                 this.comboBox1.SelectedIndex = 0;
             }
        }

        /// <summary>
        /// Time series data read
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            this.comboBox4.Items.Clear();
            this.comboBox5.Items.Clear();

            #region Open a time series data.excel file
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Title = "Open a Time series data. excel file";
            OFD.FileName = "*.xls";
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                this.comboBox4.Text = OFD.FileName.ToString();
            }
            #endregion

            #region GetTheExcel
            if (this.comboBox4.Text == null)
            {
                MessageBox.Show("Set a Time series data source");
                return;
            }

            try
            {
                OleDbConnection conn = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data source=" + this.comboBox4.Text.ToString() + ";Extended Properties=Excel 8.0;");
                conn.Open();
                string strExcel = "";
                OleDbDataAdapter myCommand = null;
                DataSet ds = null;
                strExcel = "select * from [sheet1$]";
                myCommand = new OleDbDataAdapter(strExcel, conn);
                ds = new DataSet();
                myCommand.Fill(ds);
                table = ds.Tables[0];
                conn.Close();
            }
            catch (Exception f)
            {
                MessageBox.Show("Fail to open the Time series data source;" + "Error:" + f.ToString());
                return;
            }
            #endregion

            #region GetTheFields
            this.comboBox5.Items.Clear();//Clear the items

            //DataColumn FirstColumn = table.Columns[0];
            //DataRow FirstRow = table.Rows[0];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (!String.IsNullOrEmpty(table.Columns[i].Caption))
                {
                    this.comboBox5.Items.Add(table.Columns[i].Caption.ToString());
                    this.comboBox6.Items.Add(table.Columns[i].Caption.ToString());
                }
            }

            if (this.comboBox5.Items.Count > 0)
            {
                for (int i = 0; i < this.comboBox5.Items.Count; i++)
                {
                    if (this.comboBox5.Items.ToString() == "Name" || this.comboBox5.Items.ToString() == "name")
                    {
                        this.comboBox5.SelectedIndex = i;
                        return;
                    }
                }

                this.comboBox5.SelectedIndex = 0;
            }

            if (this.comboBox6.Items.Count > 0)
            {
                this.comboBox6.SelectedIndex = 0;
            }
            #endregion
        }

        /// <summary>
        /// Get the Polygons
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Dictionary<string, IPolygon> GetNamePolygon(String MatchName)
        {
            Dictionary<string, IPolygon> NamePolygon = new Dictionary<string, IPolygon>();

            #region MainProcess
            string s1 = comboBox1.Text;
            IFeatureClass LayerFeatureClass = function.GetFeatureClass(axMapControl1.Map, s1);

            for (int i = 0; i < LayerFeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = LayerFeatureClass.GetFeature(i);
                IFields pFields = pFeature.Fields;
                int field1 = pFields.FindField(MatchName);
                String NameValue = Convert.ToString(pFeature.get_Value(field1));

                if (!NamePolygon.ContainsKey(NameValue))
                {
                    IPolygon pPolygon = pFeature.Shape as IPolygon;
                    NamePolygon.Add(NameValue, pPolygon);
                }
            }
            #endregion

            return NamePolygon;
        }

        /// <summary>
        /// 获取每个Name对应下的TimeValues
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Dictionary<string, List<double>> GetNameTimeSeries(String MatchName,string ValueName)
        {
            Dictionary<string, List<double>> NameTimeSeries = new Dictionary<string, List<double>>();

            #region MainProcess
            foreach (DataRow dr in table.Rows)
            {
                string Name = dr[MatchName].ToString();
                double Value = Convert.ToDouble(dr[ValueName]);

                if (!NameTimeSeries.ContainsKey(Name))
                {
                    List<double> ValueList = new List<double>();
                    ValueList.Add(Value);
                    NameTimeSeries.Add(Name, ValueList);
                }
                else
                {
                    NameTimeSeries[Name].Add(Value);
                }
            }

            #endregion

            return NameTimeSeries;
        }

        /// <summary>
        /// Get the Fields of PolygonData
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.comboBox3.Items.Clear();
            string s1 = comboBox1.Text;
            IFeatureClass LayerFeatureClass = function.GetFeatureClass(axMapControl1.Map, s1);

            int FeatureNum = LayerFeatureClass.FeatureCount(null);
            if (FeatureNum > 0)
            {
                IFeature pFeature = LayerFeatureClass.GetFeature(0);
                IFields pFields = pFeature.Fields;

                int fnum;
                fnum = pFields.FieldCount;

                for (int i = 0; i < fnum; i++)
                {
                    IField pField = pFields.get_Field(i);
                    string FieldName = pFields.get_Field(i).Name;
                    this.comboBox3.Items.Add(FieldName);
                }
            }

            if (this.comboBox3.Items.Count > 0)
            {
                for (int i = 0; i < this.comboBox3.Items.Count; i++)
                {
                    if (this.comboBox3.Items.ToString() == "Name" || this.comboBox3.Items.ToString() == "name")
                    {
                        this.comboBox3.SelectedIndex = i;
                        return;
                    }
                }

                this.comboBox3.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Implement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            Dictionary<string, IPolygon> NamePolygon = this.GetNamePolygon(this.comboBox3.Text.ToString());//Polygons
            Dictionary<string, List<double>> NameTimeSeries=this.GetNameTimeSeries(this.comboBox5.Text.ToString(),this.comboBox6.Text.ToString());//TimeValues

            #region 获取每一个时刻的分级信息
            List<Dictionary<IPolygon, int>> TargetTimeValueSeries = new List<Dictionary<IPolygon, int>>();
            for (int i = 0; i < NameTimeSeries.First().Value.Count; i++)
            {
                Dictionary<IPolygon, int> TargetTimeValue = new Dictionary<IPolygon, int>();//指定时刻的分级序列
                foreach (KeyValuePair<string, IPolygon> kv in NamePolygon)
                {
                    TargetTimeValue.Add(kv.Value,Convert.ToInt16(NameTimeSeries[kv.Key][i]));
                }
                TargetTimeValueSeries.Add(TargetTimeValue);
            }
            #endregion

            #region 计算每一个时刻的信息量
            Dictionary<int,double> ClassEntroyDic = new Dictionary<int,double>();//Dic for ClassEntroy
            Dictionary<int,double> MetricEntroyDic = new Dictionary<int,double>();//Dic for MetricEntroy
            Dictionary<int,double> ThematicEntroyDic = new Dictionary<int,double>();//Dic for ThematicEntroy

            PuTools.EvaPara EP = new PuTools.EvaPara();
            for (int i = 0; i < TargetTimeValueSeries.Count; i++)
            {
                double ClassEntroy = EP.ClassEntroy(TargetTimeValueSeries[i].Values.ToList());
                double MetricEntroy = EP.MapMetricEntroy1(TargetTimeValueSeries[i]);
                double ThematicEntroy = EP.MapThematicEntroy(TargetTimeValueSeries[i]);

                ClassEntroyDic.Add(i,ClassEntroy);
                MetricEntroyDic.Add(i,MetricEntroy);
                ThematicEntroyDic.Add(i,ThematicEntroy);
            }
            #endregion

            #region 计算信息量的稳定性
            double ClassEntroyChange = EP.GlobalTimeMoranI(ClassEntroyDic);
            double MetricEntroyChange = EP.GlobalTimeMoranI(MetricEntroyDic);
            double ThematicEntroyChange = EP.GlobalTimeMoranI(ThematicEntroyDic);

            List<double> LocalClassEntroyChange = EP.TimeLocalMoranIList(ClassEntroyDic);
            List<double> LocalMetricEntroyChange = EP.TimeLocalMoranIList(MetricEntroyDic);
            List<double> LocalThematicEntroyChange = EP.TimeLocalMoranIList(ThematicEntroyDic);
            #endregion

            #region OutPut
            if (localFilePath == "")
            {
                MessageBox.Show("Out path is Null");
                return;
            }

            System.IO.FileStream fs = new System.IO.FileStream(localFilePath+".txt", System.IO.FileMode.OpenOrCreate);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write("ClassEntroyChange:" + ClassEntroyChange.ToString()); sw.Write("\r\n");
            sw.Write("MetricEntroyChange:" + MetricEntroyChange.ToString()); sw.Write("\r\n");
            sw.Write("ThematicEntroyChange:" + ThematicEntroyChange.ToString()); sw.Write("\r\n");

            sw.Write("LocalClassEntroyChange:"); sw.Write("\r\n");
            for (int i = 0; i < LocalClassEntroyChange.Count; i++)
            {
                 sw.Write(LocalClassEntroyChange[i].ToString()); sw.Write("\r\n");
            }

            sw.Write("LocalMetricEntroyChange:"); sw.Write("\r\n");
            for (int i = 0; i < LocalMetricEntroyChange.Count; i++)
            {
                sw.Write(LocalMetricEntroyChange[i].ToString()); sw.Write("\r\n");
            }

            sw.Write("LocalThematicEntroyChange:"); sw.Write("\r\n");
            for (int i = 0; i < LocalThematicEntroyChange.Count; i++)
            {
                sw.Write(LocalThematicEntroyChange[i].ToString()); sw.Write("\r\n");
            }

            sw.Close();
            fs.Close();
            #endregion 
        }
    }
}
