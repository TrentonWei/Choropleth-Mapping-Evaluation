using System;
using System.Collections.Generic;
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
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using System.IO;

namespace MapAssign.PuTools
{
    class Functions
    {
        #region 获取FeatureClass
        public IFeatureClass GetFeatureClass(IMap Map, string s)
        {
            int ILayerCount;

            ILayerCount = Map.LayerCount;
            IFeatureClass pFeatureClass1 = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    ILayer Shapelayer1 = Map.get_Layer(LayerIndex1);
                    if (Shapelayer1.Name == s)
                    {
                        IFeatureLayer FeatureLayer1;
                        FeatureLayer1 = (IFeatureLayer)Shapelayer1;

                        pFeatureClass1 = FeatureLayer1.FeatureClass;
                    }
                }
            }

            return pFeatureClass1;
        }
        #endregion

        #region 创建指定形状的面文件
        public IFeatureClass createshapefile(ILayer pLayer, string filepath, string filename)
        {
            //设置字段集
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            //设置字段
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;

            //创建类型为几何类型的字段
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            //为字段创建几何定义，包括类型和空间参考
            IGeometryDefEdit pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;

            IGeoDataset pGeoDataset = pLayer as IGeoDataset;
            pGeoDefEdit.SpatialReference_2 = pGeoDataset.SpatialReference;
            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            //打开工作空间
            IWorkspaceFactory factory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace wspace = factory.OpenFromFile(filepath, 0) as IFeatureWorkspace;

            IFeatureClass out_shpfileclass = wspace.CreateFeatureClass(filename, pFields, null, null, esriFeatureType.esriFTSimple, "shape", "");
            return out_shpfileclass;
        }
        #endregion

        #region 创建指定形状的点文件
        public IFeatureClass createPointshapefile(IMap Map, string filepath, string filename)
        {
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDefEdit pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            pGeoDefEdit.SpatialReference_2 = Map.SpatialReference;
            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            IWorkspaceFactory factory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace wspace = factory.OpenFromFile(filepath, 0) as IFeatureWorkspace;

            IFeatureClass out_shpfileclass = wspace.CreateFeatureClass(filename, pFields, null, null, esriFeatureType.esriFTSimple, "shape", "");
            return out_shpfileclass;
        }
        #endregion

        #region 创建指定形状的线文件
        public IFeatureClass createLineshapefile(IMap Map, string filepath, string filename)
        {
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;
            pFieldEdit.Name_2 = "SHAPE";
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
            IGeometryDefEdit pGeoDef = new GeometryDefClass();
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            pGeoDefEdit.SpatialReference_2 = Map.SpatialReference;
            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            IWorkspaceFactory factory = new ShapefileWorkspaceFactory();
            IFeatureWorkspace wspace = factory.OpenFromFile(filepath, 0) as IFeatureWorkspace;

            IFeatureClass out_shpfileclass = wspace.CreateFeatureClass(filename, pFields, null, null, esriFeatureType.esriFTSimple, "shape", "");
            return out_shpfileclass;
        }
        #endregion

        #region 获取指定名字的图层
        public IFeatureLayer GetLayer(IMap pMap, string s)
        {
            int ILayerCount;

            ILayerCount = pMap.LayerCount;
            IFeatureLayer FeatureLayer1 = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    ILayer Shapelayer1 = pMap.get_Layer(LayerIndex1);
                    if (Shapelayer1.Name == s)
                    {
                        FeatureLayer1 = (IFeatureLayer)Shapelayer1;
                    }
                }
            }

            return FeatureLayer1;
        }
        #endregion

        #region 获取指定名字的图层
        public ILayer GetLayer1(IMap pMap, string s)
        {
            int ILayerCount;

            ILayerCount = pMap.LayerCount;
            ILayer Layer1 = null;

            if (ILayerCount <= 0)
            {
                return null;
            }

            else
            {
                for (int LayerIndex1 = 0; LayerIndex1 < ILayerCount; LayerIndex1++)
                {
                    ILayer Shapelayer1 = pMap.get_Layer(LayerIndex1);
                    if (Shapelayer1.Name == s)
                    {
                        Layer1 = (ILayer)Shapelayer1;
                    }
                }
            }

            return Layer1;
        }
        #endregion

        #region 添加字段
        public void AddField(IFeatureClass pFeatureClass, string name, esriFieldType FieldType)
        {
            //修改
            if (pFeatureClass.Fields.FindField(name) < 0)
            {
                IFeatureClass pFc = (IFeatureClass)pFeatureClass;
                IClass pClass = pFc as IClass;

                IFieldsEdit fldsE = pFc.Fields as IFieldsEdit;
                IField fld = new FieldClass();
                IFieldEdit2 fldE = fld as IFieldEdit2;
                fldE.Type_2 = FieldType;
                fldE.Name_2 = name;
                pClass.AddField(fld);
            }
        }
        #endregion

        #region 将数据存储到字段下
        public void DataStore(IFeatureClass pFeatureClass, IFeature pFeature, string s, double t)
        {
            IDataset dataset = pFeatureClass as IDataset;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit wse = workspace as IWorkspaceEdit;

            IFields pFields = pFeature.Fields;

            wse.StartEditing(false);
            wse.StartEditOperation();

            int fnum;
            fnum = pFields.FieldCount;

            for (int m = 0; m < fnum; m++)
            {
                if (pFields.get_Field(m).Name == s)
                {
                    int field1 = pFields.FindField(s);
                    pFeature.set_Value(field1, t);
                    pFeature.Store();
                }
            }

            wse.StopEditOperation();
            wse.StopEditing(true);
        }
        #endregion

        #region 创建虚拟点图层
        public IFeatureLayer CreateFeatureLayerInmemeory(IMap Map, string DataSetName)
        {
            IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
            ESRI.ArcGIS.Geodatabase.IWorkspaceName workspaceName = workspaceFactory.Create("", "MyWorkspace", null, 0);
            ESRI.ArcGIS.esriSystem.IName name = (IName)workspaceName;
            ESRI.ArcGIS.Geodatabase.IWorkspace inmemWor = (IWorkspace)name.Open();

            IFeatureClass oFeatureClass = null;
            IFeatureLayer oFeatureLayer = null;
            try
            {
                IFields pFields = new FieldsClass();
                IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

                IField pField = new FieldClass();
                IFieldEdit pFieldEdit = (IFieldEdit)pField;
                pFieldEdit.Name_2 = "SHAPE";
                pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                IGeometryDefEdit pGeoDef = new GeometryDefClass();
                IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
                pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPoint;
                pGeoDefEdit.SpatialReference_2 = Map.SpatialReference;
                pFieldEdit.GeometryDef_2 = pGeoDef;
                pFieldsEdit.AddField(pField);

                oFeatureClass = (inmemWor as IFeatureWorkspace).CreateFeatureClass(DataSetName, pFields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");
                (oFeatureClass as IDataset).BrowseName = DataSetName;
                oFeatureLayer = new FeatureLayerClass();
                //oFeatureLayer.Name = AliaseName;
                oFeatureLayer.FeatureClass = oFeatureClass;
            }

            catch
            {
                MessageBox.Show("创建虚拟图层失败");
            }

            return oFeatureLayer;
        }
        #endregion

        #region 创建虚拟线图层
        public IFeatureLayer CreateLineFeatureLayerInmemeory(IMap Map, string DataSetName)
        {
            IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
            ESRI.ArcGIS.Geodatabase.IWorkspaceName workspaceName = workspaceFactory.Create("", "MyWorkspace", null, 0);
            ESRI.ArcGIS.esriSystem.IName name = (IName)workspaceName;
            ESRI.ArcGIS.Geodatabase.IWorkspace inmemWor = (IWorkspace)name.Open();

            IFeatureClass oFeatureClass = null;
            IFeatureLayer oFeatureLayer = null;
            try
            {
                IFields pFields = new FieldsClass();
                IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

                IField pField = new FieldClass();
                IFieldEdit pFieldEdit = (IFieldEdit)pField;
                pFieldEdit.Name_2 = "SHAPE";
                pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                IGeometryDefEdit pGeoDef = new GeometryDefClass();
                IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
                pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
                pGeoDefEdit.SpatialReference_2 = Map.SpatialReference;
                pFieldEdit.GeometryDef_2 = pGeoDef;
                pFieldsEdit.AddField(pField);

                oFeatureClass = (inmemWor as IFeatureWorkspace).CreateFeatureClass(DataSetName, pFields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");
                (oFeatureClass as IDataset).BrowseName = DataSetName;
                oFeatureLayer = new FeatureLayerClass();
                //oFeatureLayer.Name = AliaseName;
                oFeatureLayer.FeatureClass = oFeatureClass;
            }

            catch
            {
                MessageBox.Show("创建虚拟图层失败");
            }

            return oFeatureLayer;
        }
        #endregion

        #region 创建虚拟面图层
        public IFeatureLayer CreatePolygonFeatureLayerInmemeory(IMap Map, string DataSetName)
        {
            IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
            ESRI.ArcGIS.Geodatabase.IWorkspaceName workspaceName = workspaceFactory.Create("", "MyWorkspace", null, 0);
            ESRI.ArcGIS.esriSystem.IName name = (IName)workspaceName;
            ESRI.ArcGIS.Geodatabase.IWorkspace inmemWor = (IWorkspace)name.Open();

            IFeatureClass oFeatureClass = null;
            IFeatureLayer oFeatureLayer = null;
            try
            {
                IFields pFields = new FieldsClass();
                IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

                IField pField = new FieldClass();
                IFieldEdit pFieldEdit = (IFieldEdit)pField;
                pFieldEdit.Name_2 = "SHAPE";
                pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                IGeometryDefEdit pGeoDef = new GeometryDefClass();
                IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
                pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
                pGeoDefEdit.SpatialReference_2 = Map.SpatialReference;
                pFieldEdit.GeometryDef_2 = pGeoDef;
                pFieldsEdit.AddField(pField);

                oFeatureClass = (inmemWor as IFeatureWorkspace).CreateFeatureClass(DataSetName, pFields, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");
                (oFeatureClass as IDataset).BrowseName = DataSetName;
                oFeatureLayer = new FeatureLayerClass();
                //oFeatureLayer.Name = AliaseName;
                oFeatureLayer.FeatureClass = oFeatureClass;
            }

            catch
            {
                MessageBox.Show("创建虚拟图层失败");
            }

            return oFeatureLayer;
        }
        #endregion
    }
}
