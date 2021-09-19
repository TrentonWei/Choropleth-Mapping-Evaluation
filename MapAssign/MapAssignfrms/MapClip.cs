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
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.AnalysisTools;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;

namespace MapAssign.MapAssignfrms
{
    public partial class MapClip : Form
    {
        public MapClip(AxMapControl mMapControl)
        {
            InitializeComponent();
            this.pMapControl = mMapControl;
        }

        #region 参数
        AxMapControl pMapControl;
        MapAssign.PuTools.Functions function = new PuTools.Functions();
        MapAssign.PuTools.symbolization Symbol = new PuTools.symbolization();
        private string OutPath;
        #endregion

        #region 初始化
        private void MapClip_Load(object sender, EventArgs e)
        {
            #region combobox1,3,4初始化
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

                        this.comboBox3.Items.Add(strLayerName);

                        if (pFeatureLayer.FeatureClass.FeatureType != esriFeatureType.esriFTAnnotation)
                        {
                            if (pFeatureLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                            {
                                this.comboBox1.Items.Add(strLayerName);
                            }
                        }
                    }

                    else if (LayerDataset.Type == esriDatasetType.esriDTRasterDataset)
                    {
                        this.comboBox4.Items.Add(pLayer.Name);
                    }
                }
            }

            if (this.comboBox1.Items.Count > 0)
            {
                this.comboBox1.SelectedIndex = 0;
            }

            if (this.comboBox3.Items.Count > 0)
            {
                this.comboBox3.SelectedIndex = 0;
            }

            if (this.comboBox4.Items.Count > 0)
            {
                this.comboBox4.SelectedIndex = 0;
            }
            #endregion

            #region Textbox1与Textbox2初始化
            this.textBox1.Text = "39";
            this.textBox2.Text = "26";
            #endregion           

            #region Listbox1与Listbox2初始化
            this.listBox1.Items.Clear();
            this.listBox2.Items.Clear();
            #endregion
        }
        #endregion

        #region 添加矢量图层到待裁剪图层中
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            //删除listbox1中的图层，并添加到listbox2中         
            string ListboxS1;
            ListboxS1 = this.comboBox3.SelectedItem.ToString();
         
            #region 判断listbox1中是否存在，存在则不添加
            int label = 0;
            int listbox1num = listBox1.Items.Count;
            if (listbox1num > 0)
            {
                for (int j = 0; j < listBox1.Items.Count; j++)
                {
                    if (listBox1.Items[j].Equals(ListboxS1))
                    {
                        label = 1;
                    }
                }

                if (label == 0)
                {
                    listBox1.Items.Add(ListboxS1);
                }
            }

            else
            {
                listBox1.Items.Add(ListboxS1);
            }
            #endregion
        }
        #endregion

        #region 添加矢量图层到待裁剪图层中
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            string ListboxS2;
            ListboxS2 = this.comboBox4.SelectedItem.ToString();

            #region 判断listbox2中是否存在，存在则不添加
            int label = 0;
            int listbox2num = listBox2.Items.Count;
            if (listbox2num > 0)
            {
                for (int j = 0; j < listBox2.Items.Count; j++)
                {
                    if (listBox2.Items[j].Equals(ListboxS2))
                    {
                        label = 1;
                    }
                }

                if (label == 0)
                {
                    listBox2.Items.Add(ListboxS2);
                }
            }

            else
            {
                listBox2.Items.Add(ListboxS2);
            }
            #endregion
        }
        #endregion

        #region 双击移除待裁剪矢量图层
        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (this.listBox1.Items.Count > 0)
            {
                this.listBox1.Items.Remove(this.listBox1.SelectedItem);
            }
        }
        #endregion

        #region 双击移除待裁剪影像图层
        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (this.listBox2.Items.Count > 0)
            {
                this.listBox2.Items.Remove(this.listBox2.SelectedItem);              
            }
        }
        #endregion

        #region 输出路径
        private void button2_Click(object sender, EventArgs e)
        {
            this.comboBox2.Items.Clear();
            FolderBrowserDialog fdialog = new FolderBrowserDialog();
            string outfilepath = null;

            if (fdialog.ShowDialog() == DialogResult.OK)
            {
                string Path = fdialog.SelectedPath;
                outfilepath = Path;
            }

            OutPath = outfilepath;
            this.comboBox2.Text = OutPath;
        }
        #endregion

        #region 考虑纸张裁剪
        private void button1_Click(object sender, EventArgs e)
        {
            #region 数据读取
            IFeatureLayer pFeatureLayer = null;
            if (this.comboBox1.Text == "")
            {
                MessageBox.Show("未选择分幅图层");
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
            IGeoDataset lGeodataset = cLayer as IGeoDataset;
            pMap.SpatialReference = lGeodataset.SpatialReference;

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

            if (this.comboBox2.Text == "")
            {
                MessageBox.Show("请给出输出路径");
                return;
            }
            #endregion

            function.AddField(pFeatureLayer.FeatureClass, "fScale", esriFieldType.esriFieldTypeDouble);//添加比例尺字段

            #region 数据分幅分组
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
                        if ((int)pFeature.get_Value(pFeature.Fields.FindField(s1)) > MapAssignNum)
                        {
                            MapAssignNum = (int)pFeature.get_Value(pFeature.Fields.FindField(s1));
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
                            if ((int)pFeature.get_Value(pFeature.Fields.FindField(s1)) == i)
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
                    double RelMaxScale = Math.Ceiling(MaxScale / 100) * 100;
                    ScaleList.Add(RelMaxScale);
                    #endregion
                    #endregion

                    #region 矩形长宽与四点坐标返回
                    double nWidth = RelMaxScale / MaxScale * eWidth;
                    double nHeight = RelMaxScale / MaxScale * eHeight;
                    a = a + (nWidth - eWidth) / 2; b = b - (nWidth - eWidth) / 2;
                    c = c + (nHeight - eHeight) / 2; d = d - (nHeight - eHeight) / 2;
                    #endregion

                    #region 矩形绘制
                    IEnvelope newEnvelope = new EnvelopeClass();
                    newEnvelope.PutCoords(b, d, a, c);
                    object PolygonSymbol = Symbol.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
                    pMapControl.DrawShape(newEnvelope, ref PolygonSymbol);
                    #endregion

                    Geoprocessor gp = new Geoprocessor();
                    gp.OverwriteOutput = true;

                    #region 创建文件夹
                    Directory.CreateDirectory(OutPath + "\\" + (i+1).ToString());
                    #endregion

                    #region 影像数据裁剪
                    try
                    {
                        for (int r = 0; r < this.listBox2.Items.Count; r++)
                        {
                            string rstring = this.listBox2.Items[r].ToString();
                            ILayer rLayer = function.GetLayer1(pMap, rstring);

                            #region 坐标转换到影像数据坐标
                            IGeoDataset cGeoDataset = cLayer as IGeoDataset;
                            IGeoDataset rGeoDataset = rLayer as IGeoDataset;
                            ISpatialReference earthref = cGeoDataset.SpatialReference;
                            ISpatialReference flatref = rGeoDataset.SpatialReference;
                            IProjectedCoordinateSystem pProCoordSys = flatref as IProjectedCoordinateSystem;

                            IPoint rPoint1 = new PointClass();
                            rPoint1.PutCoords(newEnvelope.XMin, newEnvelope.YMin);
                            IGeometry geo1 = (IGeometry)rPoint1;
                            geo1.SpatialReference = earthref;
                            geo1.Project(pProCoordSys);

                            IPoint rPoint4 = new PointClass();
                            rPoint4.PutCoords(newEnvelope.XMax, newEnvelope.YMax);
                            IGeometry geo4 = (IGeometry)rPoint4;
                            geo4.SpatialReference = earthref;
                            geo4.Project(pProCoordSys);
                            #endregion

                            ESRI.ArcGIS.DataManagementTools.Clip ClipRaster = new ESRI.ArcGIS.DataManagementTools.Clip();
                            ClipRaster.in_raster = rLayer;
                            string s = rPoint1.X + " " + rPoint1.Y + " " + rPoint4.X + " " + rPoint4.Y;
                            ClipRaster.rectangle = s;

                            ClipRaster.out_raster = OutPath + "\\" + (i+1).ToString() +"\\"+ (i+1).ToString()+rLayer.Name;
                            ClipRaster.clipping_geometry = "NONE";

                            gp.Execute(ClipRaster, null);
                        }
                    }

                    catch
                    {
                        string ms = "";
                        if (gp.MessageCount > 0)
                        {
                            for (int Count = 0; Count <= gp.MessageCount - 1; Count++)
                            {
                                ms += gp.GetMessage(Count);
                            }
                        }

                        MessageBox.Show(ms);
                    }
                    #endregion

                    #region 矢量数据裁剪
                    IFeatureClass PolygonFeatureClass = function.createshapefile(cLayer, OutPath+"\\"+(i+1).ToString(), (i+1).ToString() + "Polygon");
                    IFeatureClass MapAssignFeatureClass = function.createshapefile(cLayer, OutPath + "\\" + (i + 1).ToString(), (i + 1).ToString() + "MapAssign");
                    IDataset dataset1 = PolygonFeatureClass as IDataset;
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

                    #region 写入外接矩形
                    IFeature pfea = PolygonFeatureClass.CreateFeature();
                    pfea.Shape = pPolygon as IGeometry;
                    pfea.Store();
                    #endregion

                    #region 写入分幅图形
                    for (int j = 0; j < pMapAssign.Count;j++ )
                    {
                        IFeature mFeature = pFeatureClass.GetFeature(pMapAssign[j]);
                        IFeature mfea = MapAssignFeatureClass.CreateFeature();
                        mfea.Shape = mFeature.Shape;
                        mfea.Store();
                    }
                    #endregion

                    wse1.StopEditOperation();
                    wse1.StopEditing(true);

                    #region 将该图层添加到当前地图中
                    IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
                    IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(OutPath+"\\"+(i+1).ToString(), 0);
                    IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
                    IFeatureClass pFC = pFeatureWorkspace.OpenFeatureClass((i+1).ToString()+"Polygon");
                    IFeatureLayer pFLayer = new FeatureLayerClass();
                    pFLayer.FeatureClass = pFC;
                    pFLayer.Name = pFC.AliasName;
                    ILayer pLayer = pFLayer as ILayer;
                    pMap.AddLayer(pLayer);
                    #endregion

                    for (int v = 0; v < this.listBox1.Items.Count; v++)
                    {
                        string vstring = this.listBox1.Items[v].ToString();
                        ILayer vLayer = function.GetLayer1(pMap, vstring);

                        ESRI.ArcGIS.AnalysisTools.Clip ClipVector = new ESRI.ArcGIS.AnalysisTools.Clip();
                        ClipVector.in_features = vLayer;
                        ClipVector.clip_features = pLayer;
                        ClipVector.out_feature_class = OutPath + "\\"+(i+1).ToString()+"\\" + (i+1).ToString() + vLayer.Name + ".shape";

                        gp.Execute(ClipVector, null);
                    }

                    pMap.DeleteLayer(pLayer);               
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

            MessageBox.Show("裁剪完毕");
        }
        #endregion

        #region test
        private void button3_Click(object sender, EventArgs e)
        {
            Geoprocessor gp = new Geoprocessor();
            gp.OverwriteOutput = true;

            for (int i = 0; i < this.listBox1.Items.Count; i++)
            {
                string vstring = this.listBox1.Items[i].ToString();
                ILayer vLayer = function.GetLayer1(pMapControl.Map, vstring);

                ESRI.ArcGIS.AnalysisTools.Clip ClipVector = new ESRI.ArcGIS.AnalysisTools.Clip();
                ClipVector.in_features = vLayer;
                ClipVector.clip_features = function.GetLayer1(pMapControl.Map, this.comboBox1.Text.ToString());
                ClipVector.out_feature_class = OutPath + "\\" + i + vLayer.Name + ".shape";

                gp.Execute(ClipVector, null);
            }
        }
        #endregion

        #region 填满纸张裁剪
        private void button4_Click(object sender, EventArgs e)
        {
            #region 数据读取
            IFeatureLayer pFeatureLayer = null;
            if (this.comboBox1.Text == "")
            {
                MessageBox.Show("未选择分幅图层");
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

            double kWidth = 0; double kHeight = 0;
            double lWidth = 0; double lHeight = 0;
            IGeoDataset lGeodataset = cLayer as IGeoDataset;
            pMap.SpatialReference = lGeodataset.SpatialReference;

            if (this.textBox1.Text != "" && this.textBox2.Text != "")
            {
                kWidth = double.Parse(this.textBox1.Text.ToString());
                kHeight = double.Parse(this.textBox2.Text.ToString());
                lWidth = kWidth - 2; lHeight = kHeight - 2;
            }

            else
            {
                MessageBox.Show("请给出纸张的宽与高");
                return;
            }

            if (this.comboBox2.Text == "")
            {
                MessageBox.Show("请给出输出路径");
                return;
            }
            #endregion

            function.AddField(pFeatureLayer.FeatureClass, "fScale", esriFieldType.esriFieldTypeDouble);//添加比例尺字段

            #region 数据分幅分组
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
                        if ((int)pFeature.get_Value(pFeature.Fields.FindField(s1)) > MapAssignNum)
                        {
                            MapAssignNum = (int)pFeature.get_Value(pFeature.Fields.FindField(s1));
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
                            if ((int)pFeature.get_Value(pFeature.Fields.FindField(s1)) == i)
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
                        double wMaxScale = eWidth / lWidth * 11120000;//将度转换为meters 一度是111.2千米，一分是1853米，一秒是30.9米
                        double hMaxScale = eHeight / lHeight * 11120000;//将度转换为meters


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
                        double wMaxScale = eWidth / lWidth * 100;
                        double hMaxScale = eHeight / lHeight * 100;


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
                    double RelMaxScale = Math.Ceiling(MaxScale / 100) * 100;
                    ScaleList.Add(RelMaxScale);
                    #endregion
                    #endregion

                    #region 矩形长宽与四点坐标返回
                    double nWidth = RelMaxScale / MaxScale * eWidth;
                    double nHeight = RelMaxScale / MaxScale * eHeight;
                    double a1 = a + (nWidth - eWidth) / 2; double b1 = b - (nWidth - eWidth) / 2;
                    double c1 = c + (nHeight - eHeight) / 2; double d1 = d - (nHeight - eHeight) / 2;

                    double a2 = a + (kWidth * RelMaxScale / 11120000 - eWidth) / 2; double b2 = b - (kWidth * RelMaxScale / 11120000 - eWidth) / 2;
                    double c2 = c + (kHeight * RelMaxScale / 11120000 - eHeight) / 2; double d2 = d - (kHeight * RelMaxScale / 11120000 - eHeight) / 2;
                    #endregion

                    #region 矩形绘制
                    IEnvelope newEnvelope = new EnvelopeClass();
                    newEnvelope.PutCoords(b1, d1, a1, c1);
                    object PolygonSymbol = Symbol.PolygonSymbolization(1, 100, 100, 100, 0, 0, 20, 20);
                    pMapControl.DrawShape(newEnvelope, ref PolygonSymbol);

                    IEnvelope newEnvelope2 = new EnvelopeClass();
                    newEnvelope2.PutCoords(b2, d2, a2, c2);                   
                    #endregion

                    Geoprocessor gp = new Geoprocessor();
                    gp.OverwriteOutput = true;

                    #region 创建文件夹
                    Directory.CreateDirectory(OutPath + "\\" + (i + 1).ToString());
                    #endregion

                    #region 影像数据裁剪
                    try
                    {
                        for (int r = 0; r < this.listBox2.Items.Count; r++)
                        {
                            string rstring = this.listBox2.Items[r].ToString();
                            ILayer rLayer = function.GetLayer1(pMap, rstring);

                            #region 坐标转换到影像数据坐标
                            IGeoDataset cGeoDataset = cLayer as IGeoDataset;
                            IGeoDataset rGeoDataset = rLayer as IGeoDataset;
                            ISpatialReference earthref = cGeoDataset.SpatialReference;
                            ISpatialReference flatref = rGeoDataset.SpatialReference;
                            IProjectedCoordinateSystem pProCoordSys = flatref as IProjectedCoordinateSystem;

                            IPoint rPoint1 = new PointClass();
                            rPoint1.PutCoords(newEnvelope2.XMin, newEnvelope2.YMin);
                            IGeometry geo1 = (IGeometry)rPoint1;
                            geo1.SpatialReference = earthref;
                            geo1.Project(pProCoordSys);

                            IPoint rPoint4 = new PointClass();
                            rPoint4.PutCoords(newEnvelope2.XMax, newEnvelope2.YMax);
                            IGeometry geo4 = (IGeometry)rPoint4;
                            geo4.SpatialReference = earthref;
                            geo4.Project(pProCoordSys);
                            #endregion

                            ESRI.ArcGIS.DataManagementTools.Clip ClipRaster = new ESRI.ArcGIS.DataManagementTools.Clip();
                            ClipRaster.in_raster = rLayer;
                            string s = rPoint1.X + " " + rPoint1.Y + " " + rPoint4.X + " " + rPoint4.Y;
                            ClipRaster.rectangle = s;

                            ClipRaster.out_raster = OutPath + "\\" + (i + 1).ToString() + "\\" + (i + 1).ToString() + rLayer.Name;
                            ClipRaster.clipping_geometry = "NONE";

                            gp.Execute(ClipRaster, null);
                        }
                    }

                    catch
                    {
                        string ms = "";
                        if (gp.MessageCount > 0)
                        {
                            for (int Count = 0; Count <= gp.MessageCount - 1; Count++)
                            {
                                ms += gp.GetMessage(Count);
                            }
                        }

                        MessageBox.Show(ms);
                    }
                    #endregion

                    #region 矢量数据裁剪
                    IFeatureClass PolygonFeatureClass = function.createshapefile(cLayer, OutPath + "\\" + (i + 1).ToString(), (i + 1).ToString() + "Polygon");
                    IFeatureClass MapAssignFeatureClass = function.createshapefile(cLayer, OutPath + "\\" + (i + 1).ToString(), (i + 1).ToString() + "MapAssign");
                    IDataset dataset1 = PolygonFeatureClass as IDataset;
                    IWorkspace workspace1 = dataset1.Workspace;
                    IWorkspaceEdit wse1 = workspace1 as IWorkspaceEdit;

                    wse1.StartEditing(false);
                    wse1.StartEditOperation();

                    #region Envelope转Polygon
                    IPolygon pPolygon = new PolygonClass();
                    IPointCollection4 pPointCollection4 = pPolygon as IPointCollection4;

                    IPoint pPoint1 = new PointClass();
                    pPoint1.PutCoords(newEnvelope2.XMin, newEnvelope2.YMax);
                    pPointCollection4.AddPoint(pPoint1);

                    IPoint pPoint2 = new PointClass();
                    pPoint2.PutCoords(newEnvelope2.XMax, newEnvelope2.YMax);
                    pPointCollection4.AddPoint(pPoint2);

                    IPoint pPoint3 = new PointClass();
                    pPoint3.PutCoords(newEnvelope2.XMax, newEnvelope2.YMin);
                    pPointCollection4.AddPoint(pPoint3);

                    IPoint pPoint4 = new PointClass();
                    pPoint4.PutCoords(newEnvelope2.XMin, newEnvelope2.YMin);
                    pPointCollection4.AddPoint(pPoint4);

                    IPoint pPoint5 = new PointClass();
                    pPoint5.PutCoords(newEnvelope2.XMin, newEnvelope2.YMax);
                    pPointCollection4.AddPoint(pPoint1);
                    #endregion

                    #region 写入外接矩形
                    IFeature pfea = PolygonFeatureClass.CreateFeature();
                    pfea.Shape = pPolygon as IGeometry;
                    pfea.Store();
                    #endregion

                    #region 写入分幅图形
                    for (int j = 0; j < pMapAssign.Count; j++)
                    {
                        IFeature mFeature = pFeatureClass.GetFeature(pMapAssign[j]);
                        IFeature mfea = MapAssignFeatureClass.CreateFeature();
                        mfea.Shape = mFeature.Shape;
                        mfea.Store();
                    }
                    #endregion

                    wse1.StopEditOperation();
                    wse1.StopEditing(true);

                    #region 将该图层添加到当前地图中
                    IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactory();
                    IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(OutPath + "\\" + (i + 1).ToString(), 0);
                    IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
                    IFeatureClass pFC = pFeatureWorkspace.OpenFeatureClass((i + 1).ToString() + "Polygon");
                    IFeatureLayer pFLayer = new FeatureLayerClass();
                    pFLayer.FeatureClass = pFC;
                    pFLayer.Name = pFC.AliasName;
                    ILayer pLayer = pFLayer as ILayer;
                    pMap.AddLayer(pLayer);
                    #endregion

                    for (int v = 0; v < this.listBox1.Items.Count; v++)
                    {
                        string vstring = this.listBox1.Items[v].ToString();
                        ILayer vLayer = function.GetLayer1(pMap, vstring);

                        ESRI.ArcGIS.AnalysisTools.Clip ClipVector = new ESRI.ArcGIS.AnalysisTools.Clip();
                        ClipVector.in_features = vLayer;
                        ClipVector.clip_features = pLayer;
                        ClipVector.out_feature_class = OutPath + "\\" + (i + 1).ToString() + "\\" + (i + 1).ToString() + vLayer.Name + ".shape";

                        gp.Execute(ClipVector, null);
                    }

                    pMap.DeleteLayer(pLayer);
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

            MessageBox.Show("裁剪完毕");
        }
        #endregion

        #region test2
        private void button5_Click(object sender, EventArgs e)
        {
            ILayer Layer1 = function.GetLayer1(pMapControl.Map, this.comboBox1.Text.ToString());
            ILayer Layer2 = function.GetLayer1(pMapControl.Map, this.comboBox4.Text.ToString());
           
            IGeoDataset pGeodataset1 = Layer1 as IGeoDataset;
            IGeoDataset pGeodataset2 = Layer2 as IGeoDataset;
            pMapControl.Map.SpatialReference = pGeodataset1.SpatialReference; 
            
            IFeatureLayer pLayer = Layer1 as IFeatureLayer;
            IFeature pFeature = pLayer.FeatureClass.GetFeature(0);
            IEnvelope pEnvelope = pFeature.Shape.Envelope;

            double Xmax = pEnvelope.XMax;
            double Xmin = pEnvelope.XMin;
            double Ymax = pEnvelope.YMax;
            double Ymin = pEnvelope.YMin;
        }
        #endregion

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
