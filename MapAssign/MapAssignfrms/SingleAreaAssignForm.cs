using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using System.IO;
using ESRI.ArcGIS.esriSystem;

namespace MapAssign.MapAssignfrms
{
    public partial class SingleAreaAssignForm : Form
    {
        public SingleAreaAssignForm(AxMapControl mMapControl)
        {
            InitializeComponent();
            this.pMapControl = mMapControl;
        }

        #region 参数
        AxMapControl pMapControl;
        MapAssign.PuTools.Functions function = new PuTools.Functions();
        MapAssign.PuTools.symbolization Symbol = new PuTools.symbolization();
        string localFilePath, fileNameExt, FilePath;
        #endregion

        #region 初始化
        private void SingleAreaAssignForm_Load(object sender, EventArgs e)
        {
            #region combobox1初始化
            this.comboBox1.Items.Clear();
            IMap pMap = this.pMapControl.Map;
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
                            if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                            {
                                this.comboBox1.Items.Add(strLayerName);
                            }
                        }
                    }
                }
            }

            this.comboBox1.SelectedIndex = 0;
            #endregion

            #region combobox3初始化
            this.comboBox3.Items.Add("小全开");

            this.comboBox3.Items.Add("全开");
            this.comboBox3.Items.Add("全开（对开）");
            this.comboBox3.Items.Add("全开（三开）");
            this.comboBox3.Items.Add("全开（四开）");
            this.comboBox3.Items.Add("全开（六开）");
            this.comboBox3.Items.Add("全开（八开）");
            this.comboBox3.Items.Add("全开（十六开）");
            this.comboBox3.Items.Add("全开（三十二开）");
            this.comboBox3.Items.Add("全开（六十四开）");

            this.comboBox3.Items.Add("大全开");
            this.comboBox3.Items.Add("大全开（对开）");
            this.comboBox3.Items.Add("大全开（三开）");
            this.comboBox3.Items.Add("大全开（四开）");
            this.comboBox3.Items.Add("大全开（六开）");
            this.comboBox3.Items.Add("大全开（八开）");
            this.comboBox3.Items.Add("大全开（十六开）");
            this.comboBox3.Items.Add("大全开（三十二开）");
            this.comboBox3.Items.Add("大全开（六十四开）");
            this.comboBox3.SelectedIndex = 0;
            #endregion
        }
        #endregion

        #region 输出路径
        private void button2_Click(object sender, EventArgs e)
        {
            this.comboBox3.Items.Clear();
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = " shp files|*.shp";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //获得文件路径
                localFilePath = saveFileDialog1.FileName.ToString();

                //获取文件名，不带路径
                fileNameExt = localFilePath.Substring(localFilePath.LastIndexOf("\\") + 1);

                //获取文件路径，不带文件名
                FilePath = localFilePath.Substring(0, localFilePath.LastIndexOf("\\"));
            }

            this.comboBox4.Text = localFilePath;
        }
        #endregion

        #region 单独成幅最大比例尺计算
        /// <summary>
        ///1、 相对于纸张，各边分别留出5mm
        ///2、最大比例尺存储在MaxScale下
        ///3、取整（整100）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            #region 数据读取
            IFeatureLayer pFeatureLayer = null;
            if (this.comboBox1.Text == "")
            {
                MessageBox.Show("未选择图层");
                return;
            }

            IMap pMap = this.pMapControl.Map;
            pFeatureLayer = function.GetLayer(pMap, this.comboBox1.Text.ToString());
            if (pFeatureLayer.FeatureClass.FeatureCount(null) == 0)
            {
                MessageBox.Show("要素集为空");
                return;
            }

            double pWidth=0;double pHeight=0;
            double kWidth = 0;
            double kHeigth = 0;
            if (this.textBox1.Text != "" && this.textBox2.Text != "")
            {
                pWidth = double.Parse(this.textBox1.Text.ToString());
                pHeight = double.Parse(this.textBox2.Text.ToString());
                kWidth = pWidth - 1;
                kHeigth = pHeight - 1;
            }

            else
            {
                MessageBox.Show("请给出纸张的宽与高");
                return;
            }
            #endregion

            #region 计算每个区域的最大比例尺
            function.AddField(pFeatureLayer.FeatureClass, "MaxScale", esriFieldType.esriFieldTypeDouble);//添加比例尺字段

            for (int i = 0; i < pFeatureLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = pFeatureLayer.FeatureClass.GetFeature(i);
                if (pFeature != null)
                {
                    IEnvelope pEnvelope = pFeature.Shape.Envelope;

                    object PolygonSymbol = Symbol.PolygonSymbolization(3, 100, 100, 100, 0, 0, 20, 20);
                    pMapControl.DrawShape(pEnvelope, ref PolygonSymbol);

                    double eWidth = pEnvelope.Width;
                    double eHeight = pEnvelope.Height;
                  
                    #region 求最大比例尺
                    double MaxScale = 0;

                    if (pMap.MapUnits == esriUnits.esriDecimalDegrees)
                    {
                        double wMaxScale = eWidth / kWidth * 11120000;//将度转换为meters 一度是111.2千米，一分是1853米，一秒是30.9米
                        double hMaxScale = eHeight / kHeigth * 11120000;//将度转换为meters


                        if (wMaxScale > hMaxScale)
                        {
                            MaxScale = wMaxScale;
                        }

                        else
                        {
                            MaxScale = hMaxScale;
                        }
                    }

                    else
                    {
                        double wMaxScale = eWidth / kWidth * 100;
                        double hMaxScale = eHeight / kHeigth * 100;


                        if (wMaxScale > hMaxScale)
                        {
                            MaxScale = wMaxScale;
                        }

                        else
                        {
                            MaxScale = hMaxScale;
                        }
                    }
                    #endregion

                    #region 比例尺取整
                    if (this.checkBox1.Checked)
                    {
                        MaxScale = Math.Ceiling(MaxScale / 100) * 100;
                    }
                    #endregion

                    #region 比例尺保存
                    IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
                    IWorkspace workspace = dataset.Workspace;
                    IWorkspaceEdit wse = workspace as IWorkspaceEdit;

                    IFields pFields = pFeature.Fields;
                    wse.StartEditing(false);
                    wse.StartEditOperation();

                    int fnum;
                    fnum = pFields.FieldCount;

                    for (int m = 0; m < fnum; m++)
                    {
                        if (pFields.get_Field(m).Name == "MaxScale")
                        {
                            int field1 = pFields.FindField("MaxScale");
                            pFeature.set_Value(field1, MaxScale);
                            pFeature.Store();
                        }
                    }

                    wse.StopEditOperation();
                    wse.StopEditing(true);
                    #endregion
                }
            }
            #endregion
        }
        #endregion

        #region 纸张与纸张尺寸的联动
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox3.Text == "小全开") { this.textBox1.Text= "72"; this.textBox2.Text = "102"; }
            if (this.comboBox3.Text == "全开") { this.textBox1.Text= "78.7"; this.textBox2.Text = "109.2"; }
            if (this.comboBox3.Text == "全开（对开）") { this.textBox1.Text = "78"; this.textBox2.Text = "54"; }
            if (this.comboBox3.Text == "全开（三开）") { this.textBox1.Text = "78.7"; this.textBox2.Text = "36"; }
            if (this.comboBox3.Text == "全开（四开）") { this.textBox1.Text = "39"; this.textBox2.Text = "54"; }
            if (this.comboBox3.Text == "全开（六开）") { this.textBox1.Text = "39"; this.textBox2.Text = "36"; }
            if (this.comboBox3.Text == "全开（八开）") { this.textBox1.Text = "39"; this.textBox2.Text = "26.5"; }
            if (this.comboBox3.Text == "全开（十六开）") { this.textBox1.Text = "18.5"; this.textBox2.Text = "26"; }
            if (this.comboBox3.Text == "全开（三十二开）") { this.textBox1.Text = "18.5"; this.textBox2.Text = "13"; }
            if (this.comboBox3.Text == "全开（六十四开）") { this.textBox1.Text = "9"; this.textBox2.Text = "13"; }
            if (this.comboBox3.Text == "大全开") { this.textBox1.Text = "88.9"; this.textBox1.Text = "119.4"; }
            if (this.comboBox3.Text == "大全开（对开）") { this.textBox1.Text = "59"; this.textBox2.Text = "88"; }
            if (this.comboBox3.Text == "大全开（三开）") { this.textBox1.Text = "88.9"; this.textBox2.Text = "39"; }
            if (this.comboBox3.Text == "大全开（四开）") { this.textBox1.Text = "59"; this.textBox2.Text = "44"; }
            if (this.comboBox3.Text == "大全开（六开）") { this.textBox1.Text = "44"; this.textBox2.Text = "39"; }
            if (this.comboBox3.Text == "大全开（八开）") { this.textBox1.Text = "42"; this.textBox2.Text = "28.5"; }
            if (this.comboBox3.Text == "大全开（十六开）") { this.textBox1.Text = "21"; this.textBox2.Text = "28.5"; }
            if (this.comboBox3.Text == "大全开（三十二开）") { this.textBox1.Text = "21"; this.textBox2.Text = "14"; }
            if (this.comboBox3.Text == "大全开（六十四开）") { this.textBox1.Text = "10.5"; this.textBox2.Text = "14"; }
        }
        #endregion

        #region 指定分幅后比例尺与范围输出
        /// <summary>
        /// 1、指定分幅存储在要素的MapAssign字段中，由1开始
        /// 2、对于指定分幅求取比例尺（100取整），四周各空出5mm
        /// 3、分幅比例尺存储在字段fScale下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            #region 数据读取
            IFeatureLayer pFeatureLayer = null;
            if (this.comboBox1.Text == "")
            {
                MessageBox.Show("未选择图层");
                return;
            }

            IMap pMap = this.pMapControl.Map;
            ILayer cLayer = function.GetLayer1(pMap, this.comboBox1.Text.ToString());
            pFeatureLayer = function.GetLayer(pMap, this.comboBox1.Text.ToString());
            if (pFeatureLayer.FeatureClass.FeatureCount(null) == 0)
            {
                MessageBox.Show("要素集为空");
                return;
            }

            double pWidth = 0; double pHeight = 0;
            double kWidth = 0; double kHeigth = 0;
            if (this.textBox1.Text != "" && this.textBox2.Text != "")
            {
                pWidth = double.Parse(this.textBox1.Text.ToString());
                pHeight = double.Parse(this.textBox2.Text.ToString());
                kWidth = pWidth - 2;
                kHeigth = pHeight - 2;
            }

            else
            {
                MessageBox.Show("请给出纸张的宽与高");
                return;
            }

            if (this.comboBox4.Text == "")
            {
                MessageBox.Show("请给出输出路径");
                return;
            }
            #endregion

           function.AddField(pFeatureLayer.FeatureClass, "fScale", esriFieldType.esriFieldTypeDouble);//添加比例尺字段
           IFeatureClass EnvelopeFeatureClass= function.createshapefile(cLayer, FilePath, fileNameExt);//创建矩形框存储文件

            #region 分幅分组
            short MapAssignNum = 0;
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            for (int i = 0; i < pFeatureClass.FeatureCount(null);i++ )
            {
                IFeature pFeature = pFeatureClass.GetFeature(i);
                IFields pFields = pFeature.Fields;
                for (int j = 0; j < pFields.FieldCount; j++)
                {
                    string s1 = pFields.get_Field(j).Name;
                    if (s1=="MapAssign" )
                    {
                        if ((short)pFeature.get_Value(pFeature.Fields.FindField(s1)) > MapAssignNum)
                        {
                            MapAssignNum = (short)pFeature.get_Value(pFeature.Fields.FindField(s1));
                        }
                    }
                }
            }

            List<List<int>> MapAssignList = new List<List<int>>();
            for (int i = 1; i < MapAssignNum + 1; i++)
            {
                List<int> pMapAssign = new List<int>();
                for (int k = 0; k < pFeatureClass.FeatureCount(null); k++)
                {
                    IFeature pFeature = pFeatureClass.GetFeature(k);
                    IFields pFields = pFeature.Fields;
                    for (int j = 0; j < pFields.FieldCount; j++)
                    {
                        string s1 = pFields.get_Field(j).Name;
                        if (s1 == "MapAssign")
                        {
                            if ((short)pFeature.get_Value(pFeature.Fields.FindField(s1)) == i)
                            {
                                pMapAssign.Add(k);
                            }
                        }
                    }
                }

                MapAssignList.Add(pMapAssign);
            }
            #endregion

            #region 计算每个分幅的比例尺和矩形框
            List<double> ScaleList = new List<double>();
            for (int i = 0; i < MapAssignList.Count; i++)
            {
                List<int> pMapAssign = MapAssignList[i];

                if (pMapAssign.Count > 0)
                {
                    IFeature pFeature1 = pFeatureClass.GetFeature(pMapAssign[0]);
                    IEnvelope pEnvelope1 = pFeature1.Shape.Envelope;
                    double a = pEnvelope1.XMax;//XMax
                    double b = pEnvelope1.XMin;//XMin
                    double c = pEnvelope1.YMax;//YMax
                    double d = pEnvelope1.YMin;//YMin

                    #region 计算分幅的最大长宽
                    for (int j = 1; j < pMapAssign.Count; j++)
                    {
                        IFeature pFeature2 = pFeatureClass.GetFeature(pMapAssign[j]);
                        IEnvelope pEnvelope2 = pFeature2.Shape.Envelope;

                        if (a < pEnvelope2.XMax)
                        {
                            a = pEnvelope2.XMax;
                        }

                        if (b > pEnvelope2.XMin)
                        {
                            b = pEnvelope2.XMin;
                        }

                        if (c < pEnvelope2.YMax)
                        {
                            c = pEnvelope2.YMax;
                        }

                        if (d > pEnvelope2.YMin)
                        {
                            d = pEnvelope2.YMin;
                        }
                    }

                    double eWidth = a - b;//算矩形长宽
                    double eHeight = c - d;//算矩形长宽
                    #endregion

                    #region 最大比例尺计算/取整比例尺计算（整百）
                    double MaxScale = 0;

                    #region 最大比例尺计算
                    if (pMap.MapUnits == esriUnits.esriDecimalDegrees)
                    {
                        double wMaxScale = eWidth / kWidth * 11120000;//将度转换为meters 一度是111.2千米，一分是1853米，一秒是30.9米
                        double hMaxScale = eHeight / kHeigth * 11120000;//将度转换为meters


                        if (wMaxScale > hMaxScale)
                        {
                            MaxScale = wMaxScale;
                        }

                        else
                        {
                            MaxScale = hMaxScale;
                        }
                    }

                    else
                    {
                        double wMaxScale = eWidth / kWidth * 100;
                        double hMaxScale = eHeight / kHeigth * 100;


                        if (wMaxScale > hMaxScale)
                        {
                            MaxScale = wMaxScale;
                        }

                        else
                        {
                            MaxScale = hMaxScale;
                        }
                    }
                    #endregion

                    #region 取整比例尺计算
                    double RelMaxScale = 0;
                    if (this.checkBox1.Checked)
                    {
                        RelMaxScale = Math.Ceiling(MaxScale / 100) * 100;
                        ScaleList.Add(RelMaxScale);
                    }

                    if (this.checkBox2.Checked)
                    {
                        RelMaxScale = Math.Ceiling(MaxScale / 1000) * 1000;
                        ScaleList.Add(RelMaxScale);
                    }

                    if (this.checkBox3.Checked)
                    {
                        RelMaxScale = MaxScale;
                        ScaleList.Add(RelMaxScale);
                    }
                    #endregion
                    #endregion

                    #region 矩形长宽与四点坐标返回                   
                    double a1 = a + (pWidth * RelMaxScale / 11120000 - eWidth) / 2; double b1 = b - (pWidth*RelMaxScale/11120000 - eWidth) / 2;
                    double c1 = c + (pHeight*RelMaxScale/11120000 - eHeight) / 2; double d1 = d - (pHeight*RelMaxScale/11120000 - eHeight) / 2;
                    #endregion

                    #region 矩形绘制
                    IEnvelope newEnvelope = new EnvelopeClass();
                    newEnvelope.PutCoords(b1, d1, a1, c1);
                    object PolygonSymbol = Symbol.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
                    pMapControl.DrawShape(newEnvelope, ref PolygonSymbol);
                    #endregion

                    #region 矩形存储
                    IDataset dataset1 = EnvelopeFeatureClass as IDataset;
                    IWorkspace workspace1 = dataset1.Workspace;
                    IWorkspaceEdit wse1 = workspace1 as IWorkspaceEdit;

                    wse1.StartEditing(false);
                    wse1.StartEditOperation();

                    #region Envelope转Polygon
                    IPolygon pPolygon = new PolygonClass();
                    IPointCollection4 pPointCollection4 = pPolygon as IPointCollection4;

                    IPoint pPoint1 = new PointClass();
                    pPoint1.PutCoords(newEnvelope.XMin, newEnvelope.YMax);
                    pPointCollection4.AddPoint(pPoint1);

                    IPoint pPoint2 = new PointClass();
                    pPoint2.PutCoords(newEnvelope.XMax, newEnvelope.YMax);
                    pPointCollection4.AddPoint(pPoint2);

                    IPoint pPoint3 = new PointClass();
                    pPoint3.PutCoords(newEnvelope.XMax, newEnvelope.YMin);
                    pPointCollection4.AddPoint(pPoint3);

                    IPoint pPoint4 = new PointClass();
                    pPoint4.PutCoords(newEnvelope.XMin, newEnvelope.YMin);
                    pPointCollection4.AddPoint(pPoint4);

                    IPoint pPoint5 = new PointClass();
                    pPoint5.PutCoords(newEnvelope.XMin, newEnvelope.YMax);
                    pPointCollection4.AddPoint(pPoint1);
                    #endregion

                    IFeature pfea = EnvelopeFeatureClass.CreateFeature();
                    pfea.Shape = pPolygon as IGeometry;
                    pfea.Store();

                    wse1.StopEditOperation();
                    wse1.StopEditing(true);                  
                    #endregion
                }
            }
            #endregion

            #region 比例尺存储 
            IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            wse.StartEditing(false);
            wse.StartEditOperation();
            for (int i = 0; i < MapAssignList.Count; i++)
            {
                List<int> pMapAssign = MapAssignList[i];

                if (pMapAssign.Count > 0)
                {
                    for (int j = 0; j < pMapAssign.Count; j++)
                    {
                     
                        IFeature pFeature = pFeatureClass.GetFeature(pMapAssign[j]);
                        IFields pFields = pFeature.Fields;

                        int fnum;
                        fnum = pFields.FieldCount;

                        for (int m = 0; m < fnum; m++)
                        {
                            if (pFields.get_Field(m).Name == "fScale")
                            {
                                int field1 = pFields.FindField("fScale");
                                pFeature.set_Value(field1, ScaleList[i]);
                                pFeature.Store();
                            }
                        }                                      
                    }
                }               
            }

            wse.StopEditOperation();
            wse.StopEditing(true);    
            #endregion
        }
        #endregion
        
        #region 考虑两种纸张分幅
        private void button4_Click(object sender, EventArgs e)
        {
            #region 数据读取
            IFeatureLayer pFeatureLayer = null;
            if (this.comboBox1.Text == "")
            {
                MessageBox.Show("未选择图层");
                return;
            }

            IMap pMap = this.pMapControl.Map;
            ILayer cLayer = function.GetLayer1(pMap, this.comboBox1.Text.ToString());
            pFeatureLayer = function.GetLayer(pMap, this.comboBox1.Text.ToString());
            if (pFeatureLayer.FeatureClass.FeatureCount(null) == 0)
            {
                MessageBox.Show("要素集为空");
                return;
            }

            double pWidth = 0; double pHeight = 0;
            double kWidth = 0; double kHeigth = 0;
            if (this.textBox1.Text != "" && this.textBox2.Text != "")
            {
                pWidth = double.Parse(this.textBox1.Text.ToString());
                pHeight = double.Parse(this.textBox2.Text.ToString());
            }

            else
            {
                MessageBox.Show("请给出纸张的宽与高");
                return;
            }

            if (this.comboBox4.Text == "")
            {
                MessageBox.Show("请给出输出路径");
                return;
            }
            #endregion

            function.AddField(pFeatureLayer.FeatureClass, "fScale", esriFieldType.esriFieldTypeDouble);//添加比例尺字段
            IFeatureClass EnvelopeFeatureClass = function.createshapefile(cLayer, FilePath, fileNameExt);//创建矩形框存储文件

            #region 分幅分组
            int MapAssignNum = 0;
            IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
            for (int i = 0; i < pFeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = pFeatureClass.GetFeature(i);
                IFields pFields = pFeature.Fields;
                for (int j = 0; j < pFields.FieldCount; j++)
                {
                    string s1 = pFields.get_Field(j).Name;
                    if (s1 == "MapAssign")
                    {
                        if (int.Parse(pFeature.get_Value(pFeature.Fields.FindField(s1)).ToString()) > MapAssignNum)
                        {
                            MapAssignNum = int.Parse(pFeature.get_Value(pFeature.Fields.FindField(s1)).ToString());
                        }
                    }
                }
            }

            List<List<int>> MapAssignList = new List<List<int>>();
            for (int i = 1; i < MapAssignNum + 1; i++)
            {
                List<int> pMapAssign = new List<int>();
                for (int k = 0; k < pFeatureClass.FeatureCount(null); k++)
                {
                    IFeature pFeature = pFeatureClass.GetFeature(k);
                    IFields pFields = pFeature.Fields;
                    for (int j = 0; j < pFields.FieldCount; j++)
                    {
                        string s1 = pFields.get_Field(j).Name;
                        if (s1 == "MapAssign")
                        {
                            if (int.Parse(pFeature.get_Value(pFeature.Fields.FindField(s1)).ToString()) == i)
                            {
                                pMapAssign.Add(k);
                            }
                        }
                    }
                }

                MapAssignList.Add(pMapAssign);
            }
            #endregion

            #region 计算每个分幅的比例尺和矩形框
            List<double> ScaleList = new List<double>();
            for (int i = 0; i < MapAssignList.Count; i++)
            {
                List<int> pMapAssign = MapAssignList[i];

                if (pMapAssign.Count > 0)
                {
                    IFeature pFeature1 = pFeatureClass.GetFeature(pMapAssign[0]);
                    if (double.Parse(pFeature1.get_Value(pFeature1.Fields.FindField("展开页")).ToString()) == 1)
                    {
                        kWidth = pWidth - 2;
                        kHeigth = pHeight - 2;
                    }

                    if (double.Parse(pFeature1.get_Value(pFeature1.Fields.FindField("展开页")).ToString()) == 0.5)
                    {
                        kWidth = pWidth / 2 - 2;
                        kHeigth = pHeight - 2;
                    }

                    IEnvelope pEnvelope1 = pFeature1.Shape.Envelope;
                    double a = pEnvelope1.XMax;//XMax
                    double b = pEnvelope1.XMin;//XMin
                    double c = pEnvelope1.YMax;//YMax
                    double d = pEnvelope1.YMin;//YMin

                    #region 计算分幅的最大长宽
                    for (int j = 1; j < pMapAssign.Count; j++)
                    {
                        IFeature pFeature2 = pFeatureClass.GetFeature(pMapAssign[j]);             
                        IEnvelope pEnvelope2 = pFeature2.Shape.Envelope;

                        if (a < pEnvelope2.XMax)
                        {
                            a = pEnvelope2.XMax;
                        }

                        if (b > pEnvelope2.XMin)
                        {
                            b = pEnvelope2.XMin;
                        }

                        if (c < pEnvelope2.YMax)
                        {
                            c = pEnvelope2.YMax;
                        }

                        if (d > pEnvelope2.YMin)
                        {
                            d = pEnvelope2.YMin;
                        }
                    }

                    double eWidth = a - b;//算矩形长宽
                    double eHeight = c - d;//算矩形长宽
                    #endregion

                    #region 最大比例尺计算/取整比例尺计算（整百）
                    double MaxScale = 0;

                    #region 最大比例尺计算
                    if (pMap.MapUnits == esriUnits.esriDecimalDegrees)
                    {
                        double wMaxScale = eWidth / kWidth * 11120000;//将度转换为meters 一度是111.2千米，一分是1853米，一秒是30.9米
                        double hMaxScale = eHeight / kHeigth * 11120000;//将度转换为meters


                        if (wMaxScale > hMaxScale)
                        {
                            MaxScale = wMaxScale;
                        }

                        else
                        {
                            MaxScale = hMaxScale;
                        }
                    }

                    else
                    {
                        double wMaxScale = eWidth / kWidth * 100;
                        double hMaxScale = eHeight / kHeigth * 100;


                        if (wMaxScale > hMaxScale)
                        {
                            MaxScale = wMaxScale;
                        }

                        else
                        {
                            MaxScale = hMaxScale;
                        }
                    }
                    #endregion

                    #region 取整比例尺计算
                    double RelMaxScale = 0;
                    if (this.checkBox1.Checked)
                    {
                        RelMaxScale = Math.Ceiling(MaxScale / 100) * 100;
                        ScaleList.Add(RelMaxScale);
                    }

                    if (this.checkBox2.Checked)
                    {
                        RelMaxScale = Math.Ceiling(MaxScale / 1000) * 1000;
                        ScaleList.Add(RelMaxScale);
                    }

                    if (this.checkBox3.Checked)
                    {
                        RelMaxScale = MaxScale;
                        ScaleList.Add(RelMaxScale);
                    }
                    #endregion
                    #endregion

                    #region 矩形长宽与四点坐标返回
                    double a1 = 0; double b1 = 0; double c1 = 0; double d1 = 0;
                    if (kWidth == pWidth / 2 - 2)
                    {
                        a1 = a + (pWidth / 2 * RelMaxScale / 11120000 - eWidth) / 2; b1 = b - (pWidth / 2 * RelMaxScale / 11120000 - eWidth) / 2;
                        c1 = c + (pHeight * RelMaxScale / 11120000 - eHeight) / 2; d1 = d - (pHeight * RelMaxScale / 11120000 - eHeight) / 2;
                    }

                    if (kWidth == pWidth - 2)
                    {
                        a1 = a + (pWidth * RelMaxScale / 11120000 - eWidth) / 2; b1 = b - (pWidth * RelMaxScale / 11120000 - eWidth) / 2;
                        c1 = c + (pHeight * RelMaxScale / 11120000 - eHeight) / 2; d1 = d - (pHeight * RelMaxScale / 11120000 - eHeight) / 2;
                    }
                    #endregion

                    #region 矩形绘制
                    IEnvelope newEnvelope = new EnvelopeClass();
                    newEnvelope.PutCoords(b1, d1, a1, c1);
                    object PolygonSymbol = Symbol.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
                    pMapControl.DrawShape(newEnvelope, ref PolygonSymbol);
                    #endregion

                    #region 矩形存储
                    IDataset dataset1 = EnvelopeFeatureClass as IDataset;
                    IWorkspace workspace1 = dataset1.Workspace;
                    IWorkspaceEdit wse1 = workspace1 as IWorkspaceEdit;

                    wse1.StartEditing(false);
                    wse1.StartEditOperation();

                    #region Envelope转Polygon
                    IPolygon pPolygon = new PolygonClass();
                    IPointCollection4 pPointCollection4 = pPolygon as IPointCollection4;

                    IPoint pPoint1 = new PointClass();
                    pPoint1.PutCoords(newEnvelope.XMin, newEnvelope.YMax);
                    pPointCollection4.AddPoint(pPoint1);

                    IPoint pPoint2 = new PointClass();
                    pPoint2.PutCoords(newEnvelope.XMax, newEnvelope.YMax);
                    pPointCollection4.AddPoint(pPoint2);

                    IPoint pPoint3 = new PointClass();
                    pPoint3.PutCoords(newEnvelope.XMax, newEnvelope.YMin);
                    pPointCollection4.AddPoint(pPoint3);

                    IPoint pPoint4 = new PointClass();
                    pPoint4.PutCoords(newEnvelope.XMin, newEnvelope.YMin);
                    pPointCollection4.AddPoint(pPoint4);

                    IPoint pPoint5 = new PointClass();
                    pPoint5.PutCoords(newEnvelope.XMin, newEnvelope.YMax);
                    pPointCollection4.AddPoint(pPoint1);
                    #endregion

                    IFeature pfea = EnvelopeFeatureClass.CreateFeature();
                    pfea.Shape = pPolygon as IGeometry;
                    pfea.Store();

                    wse1.StopEditOperation();
                    wse1.StopEditing(true);
                    #endregion
                }
            }
            #endregion

            #region 比例尺存储
            IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            wse.StartEditing(false);
            wse.StartEditOperation();
            for (int i = 0; i < MapAssignList.Count; i++)
            {
                List<int> pMapAssign = MapAssignList[i];

                if (pMapAssign.Count > 0)
                {
                    for (int j = 0; j < pMapAssign.Count; j++)
                    {

                        IFeature pFeature = pFeatureClass.GetFeature(pMapAssign[j]);
                        IFields pFields = pFeature.Fields;

                        int fnum;
                        fnum = pFields.FieldCount;

                        for (int m = 0; m < fnum; m++)
                        {
                            if (pFields.get_Field(m).Name == "fScale")
                            {
                                int field1 = pFields.FindField("fScale");
                                pFeature.set_Value(field1, ScaleList[i]);
                                pFeature.Store();
                            }
                        }
                    }
                }
            }

            wse.StopEditOperation();
            wse.StopEditing(true);
            #endregion
        }
        #endregion

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
