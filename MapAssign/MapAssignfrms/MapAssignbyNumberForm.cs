﻿using System;
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
    public partial class MapAssignbyNumberForm : Form
    {
        public MapAssignbyNumberForm(AxMapControl mMapControl)
        {
            InitializeComponent();
            this.pMapControl = mMapControl;
        }

        #region 参数
        AxMapControl pMapControl;
        MapAssign.PuTools.Functions function = new PuTools.Functions();
        #endregion

        #region 裁剪方法1
        /// <summary>
        /// 1、计算建筑物邻接关系
        /// 2、对邻接关系赋值（不邻接赋值无穷大；相交或邻接赋值多边形面积平均值）
        /// 3、对构成的邻接关系计算最小生成树
        /// 4、对最小生成树的边长按从小到大裁剪
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
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

           
            int AreaNumber = int.Parse(this.comboBox2.Text.ToString());
            int MaxNumber=int.Parse(this.comboBox4.ToString());
            #endregion

            #region 面积计算
            List<double> AreaList = new List<double>();
            for (int i = 0; i < pFeatureLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                IArea pArea = pPolygon as IArea;
                double nArea = pArea.Area;
                AreaList.Add(nArea);
            }
            #endregion

            #region 邻接关系计算
            int FeatureNum = pFeatureLayer.FeatureClass.FeatureCount(null);
            int[,] matrixGraph = new int[FeatureNum, FeatureNum];

            #region 矩阵初始化(不相邻用0表示)
            for (int i = 0; i < FeatureNum; i++)
            {
                for (int j = 0; j < FeatureNum; j++)
                {
                    matrixGraph[i, j] = 0;
                }
            }
            #endregion

            #region 矩阵赋值(0表示不邻接；1表示相交或邻接；2表示存在包含关系)
            for (int i = 0; i < FeatureNum; i++)
            {
                IFeature pFeature1 = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon1 = pFeature1.Shape as IPolygon;
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (j != i)
                    {
                        IFeature pFeature2 = pFeatureLayer.FeatureClass.GetFeature(j);
                        IPolygon pPolygon2 = pFeature2.Shape as IPolygon;
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry0Dimension);
                        IGeometry iGeo2 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if(!iGeo1.IsEmpty||!iGeo2.IsEmpty)
                        {
                            matrixGraph[i, j] = matrixGraph[j, i] = 1;
                        }
                    }
                }
            }
            #endregion
            #endregion

            #region 根据面积对矩阵进行赋值（不邻接赋值无穷大；相交或邻接按邻接两多边形面积的平均值计算；包含赋值为0）
            double[,] AreamatrixGraph = new double[FeatureNum, FeatureNum];
            for (int i = 0; i < FeatureNum; i++)
            {
                for (int j = 0; j <FeatureNum; j++)
                {
                    if (matrixGraph[i, j] == 0)
                    {
                        AreamatrixGraph[i, j] = 100000000000;
                    }
                
                    if (matrixGraph[i, j] == 1)
                    {
                        AreamatrixGraph[i, j] = (AreaList[i] + AreaList[j]) / 2;
                    }
                }
            }
            #endregion

            #region 生成最小生成树
            List<List<int>> EdgesGroup = new List<List<int>>();//存储生成的MST
            IArray LabelArray = new ArrayClass();
            IArray fLabelArray = new ArrayClass();

            for (int F = 1; F < FeatureNum; F++)
            {
                fLabelArray.Add(F);
            }

            int LabelFirst = 0;
            LabelArray.Add(LabelFirst);
            //int x = 0;
            while(fLabelArray.Count>0)
            {
                double MinDist = 100000000001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                #region 寻找权值最小的一条边加入
                for (int i = 0; i < LabelArray.Count; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArray.Count; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (AreamatrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = AreamatrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }
                #endregion

                //x++;
              
                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2);
 
            }
            #endregion

            #region 最小生成树分割
            #region 建筑物聚类存储
            List<List<int>> AreaCluster = new List<List<int>>();//存储建筑物团
            List<int> Cluster = new List<int>();
            for (int i = 0; i < FeatureNum; i++)
            {
                Cluster.Add(i);
            }
            AreaCluster.Add(Cluster);
            #endregion

            #region 计算最小生成树中每条边的边长，并进行排序
            List<double> LengthList = new List<double>();
            for (int i = 0; i < EdgesGroup.Count; i++)
            {
                LengthList.Add(AreamatrixGraph[EdgesGroup[i][0], EdgesGroup[i][1]]);
            }

            LengthList.Sort();//排序
            #endregion

            #region 依次对最小生成树进行剪枝
            for (int i = 0; i < LengthList.Count; i++)
            {
                #region 移除当前EdgeGroup中的最长边
                for (int j = 0; j < EdgesGroup.Count; j++)
                {
                    if (AreamatrixGraph[EdgesGroup[j][0], EdgesGroup[j][1]] == LengthList[LengthList.Count-1-i])
                    {
                        EdgesGroup.RemoveAt(j);
                        break;
                    }
                }
                #endregion

                #region 回溯获得每个Cluster

                List<int> pAreaNumber = new List<int>();
                for (int j = 0; j < FeatureNum; j++)
                {
                    pAreaNumber.Add(j);
                }
                AreaCluster.Clear();

                for (int k = 0; k < i + 2; k++)
                {
                    List<int> pCluster = new List<int>();
                    List<int> fCluster = new List<int>();
                    pCluster.Add(pAreaNumber[0]); //任取一个节点作为首节点，遍历树
                    fCluster.Add(pAreaNumber[0]);
                    pAreaNumber.RemoveAt(0);

                    do
                    {
                        int pNumber = pCluster[0]; pCluster.RemoveAt(0);

                        for (int m = 0; m < EdgesGroup.Count; m++)
                        {
                            List<int> pEdge = EdgesGroup[m];

                            if (pNumber == pEdge[0])
                            {
                                if (!fCluster.Contains(pEdge[1]))
                                {
                                    pCluster.Add(pEdge[1]);
                                    fCluster.Add(pEdge[1]);
                                    pAreaNumber.Remove(pEdge[1]);
                                }
                            }

                            else if (pNumber == pEdge[1])
                            {
                                if (!fCluster.Contains(pEdge[0]))
                                {
                                    pCluster.Add(pEdge[0]);
                                    fCluster.Add(pEdge[0]);
                                    pAreaNumber.Remove(pEdge[0]);
                                }
                            }
                        }
                    } while (pCluster.Count > 0);

                    AreaCluster.Add(fCluster);
                }
                #endregion

                #region 判断当前cluster是否满足约束条件
                if (this.checkBox1.Checked)
                {
                    bool StopLabel = false;
                    for (int n = 0; n < AreaCluster.Count; n++)
                    {
                        if (AreaCluster[n].Count > AreaNumber)
                        {
                            StopLabel = true;
                        }
                    }

                    if (!StopLabel)
                    {
                        break;
                    }
                }

                if (this.checkBox2.Checked)
                {
                    if (AreaCluster.Count >= MaxNumber)
                    {
                        break;
                    }
                }
                #endregion
            }
            #endregion
            #endregion

            #region 将建筑物团分类要求添加到字段中
            function.AddField(pFeatureLayer.FeatureClass, "CluLab", esriFieldType.esriFieldTypeInteger);
            for (int i = 0; i < AreaCluster.Count; i++)
            {
                List<int> kCluster = AreaCluster[i];
                for (int j = 0; j < kCluster.Count; j++)
                {
                    IFeature kFeature = pFeatureLayer.FeatureClass.GetFeature(kCluster[j]);
                    IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
                    IWorkspace workspace = dataset.Workspace;
                    IWorkspaceEdit wse = workspace as IWorkspaceEdit;

                    IFields pFields = kFeature.Fields;
                    wse.StartEditing(false);
                    wse.StartEditOperation();

                    int fnum;
                    fnum = pFields.FieldCount;

                    for (int m = 0; m < fnum; m++)
                    {
                        if (pFields.get_Field(m).Name == "CluLab")
                        {
                            int field1 = pFields.FindField("CluLab");
                            kFeature.set_Value(field1, i);
                            kFeature.Store();
                        }
                    }

                    wse.StopEditOperation();
                    wse.StopEditing(true);
                }
            }
            #endregion
        }
        #endregion 

        #region 初始化
        private void MapAssignbyNumberForm_Load(object sender, EventArgs e)
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

            #region combobox2初始化
            this.comboBox2.Items.Add("1");
            this.comboBox2.Items.Add("2");
            this.comboBox2.Items.Add("3");
            this.comboBox2.Items.Add("4");
            this.comboBox2.Items.Add("5");
            this.comboBox2.Items.Add("6");
            this.comboBox2.Items.Add("7");
            this.comboBox2.Items.Add("8");
            this.comboBox2.Items.Add("9");
            this.comboBox2.Items.Add("10");
            this.comboBox2.SelectedIndex = 1;
            #endregion

            #region combobox4初始化
            this.comboBox4.Items.Add("1");
            this.comboBox4.Items.Add("2");
            this.comboBox4.Items.Add("4");
            this.comboBox4.Items.Add("8");
            this.comboBox4.Items.Add("16");
            this.comboBox4.Items.Add("32");
            this.comboBox4.Items.Add("64");
            this.comboBox4.Items.Add("128");
            this.comboBox4.SelectedIndex = 1;
            #endregion
        }
        #endregion

        #region 裁剪方法2
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


            int AreaNumber = int.Parse(this.comboBox2.Text.ToString());
            int MaxNumber = int.Parse(this.comboBox4.Text.ToString());
            #endregion

            #region 面积计算
            List<double> AreaList = new List<double>();
            for (int i = 0; i < pFeatureLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                IArea pArea = pPolygon as IArea;
                double nArea = pArea.Area;
                AreaList.Add(nArea);
            }
            #endregion

            #region 邻接关系计算
            int FeatureNum = pFeatureLayer.FeatureClass.FeatureCount(null);
            int[,] matrixGraph = new int[FeatureNum, FeatureNum];

            #region 矩阵初始化(不相邻用0表示)
            for (int i = 0; i < FeatureNum; i++)
            {
                for (int j = 0; j < FeatureNum; j++)
                {
                    matrixGraph[i, j] = 0;
                }
            }
            #endregion

            #region 矩阵赋值(0表示不邻接；1表示相交或邻接；2表示存在包含关系)
            for (int i = 0; i < FeatureNum; i++)
            {
                IFeature pFeature1 = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon1 = pFeature1.Shape as IPolygon;
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (j != i)
                    {
                        IFeature pFeature2 = pFeatureLayer.FeatureClass.GetFeature(j);
                        IPolygon pPolygon2 = pFeature2.Shape as IPolygon;
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry0Dimension);
                        IGeometry iGeo2 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if (!iGeo1.IsEmpty || !iGeo2.IsEmpty)
                        {
                            matrixGraph[i, j] = matrixGraph[j, i] = 1;
                        }
                    }
                }
            }
            #endregion
            #endregion

            #region 根据面积对矩阵进行赋值（不邻接赋值无穷大；相交或邻接按邻接两多边形面积的平均值计算；包含赋值为0）
            double[,] AreamatrixGraph = new double[FeatureNum, FeatureNum];
            for (int i = 0; i < FeatureNum; i++)
            {
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (matrixGraph[i, j] == 0)
                    {
                        AreamatrixGraph[i, j] = 100000000000;
                    }

                    if (matrixGraph[i, j] == 1)
                    {
                        AreamatrixGraph[i, j] = (AreaList[i] + AreaList[j]) / 2;
                    }
                }
            }
            #endregion

            #region 生成最小生成树
            List<List<int>> EdgesGroup = new List<List<int>>();//存储生成的MST
            IArray LabelArray = new ArrayClass();
            IArray fLabelArray = new ArrayClass();

            for (int F = 1; F < FeatureNum; F++)
            {
                fLabelArray.Add(F);
            }

            int LabelFirst = 0;
            LabelArray.Add(LabelFirst);
            //int x = 0;
            while (fLabelArray.Count > 0)
            {
                double MinDist = 100000000001;
                List<int> Edge = new List<int>();

                int EdgeLabel2 = -1;
                int EdgeLabel1 = -1;
                int Label = -1;

                #region 寻找权值最小的一条边加入
                for (int i = 0; i < LabelArray.Count; i++)
                {
                    int p1 = (int)LabelArray.get_Element(i);

                    for (int j = 0; j < fLabelArray.Count; j++)
                    {
                        int p2 = (int)fLabelArray.get_Element(j);

                        if (AreamatrixGraph[p1, p2] < MinDist)
                        {
                            MinDist = AreamatrixGraph[p1, p2];
                            EdgeLabel2 = p2;
                            EdgeLabel1 = p1;
                            Label = j;
                        }
                    }
                }
                #endregion

                //x++;

                Edge.Add(EdgeLabel1);
                Edge.Add(EdgeLabel2);
                EdgesGroup.Add(Edge);

                fLabelArray.Remove(Label);
                LabelArray.Add(EdgeLabel2);

            }
            #endregion

            #region 最小生成树分割
            #region 建筑物聚类存储
            List<List<int>> AreaCluster = new List<List<int>>();//存储建筑物团
            List<int> Cluster = new List<int>();
            for (int i = 0; i < FeatureNum; i++)
            {
                Cluster.Add(i);
            }
            AreaCluster.Add(Cluster);
            #endregion

            #region 计算最小生成树中每条边的边长，并进行排序
            List<double> LengthList = new List<double>();
            for (int i = 0; i < EdgesGroup.Count; i++)
            {
                LengthList.Add(AreamatrixGraph[EdgesGroup[i][0], EdgesGroup[i][1]]);
            }

            LengthList.Sort();//排序
            #endregion

            #region 依次对最小生成树进行剪枝
            int ClusterSum = 1;//标识聚类的总数
            for (int i = 0; i < LengthList.Count; i++)
            {
                #region 移除当前EdgeGroup中的最长边（若当前边是任意只有n个区域类中的边，则不对该边做删除）
                for (int j = 0; j < EdgesGroup.Count; j++)
                {
                    bool RemoveLabel = false;
                    if (AreamatrixGraph[EdgesGroup[j][0], EdgesGroup[j][1]] == LengthList[LengthList.Count - 1 - i])
                    {
                        for (int c = 0; c < AreaCluster.Count; c++)
                        {
                            if (AreaCluster[c].Contains(EdgesGroup[j][0]) && AreaCluster[c].Contains(EdgesGroup[j][1]))
                            {
                                if (AreaCluster[c].Count > AreaNumber)
                                {
                                     EdgesGroup.RemoveAt(j);
                                     ClusterSum = ClusterSum + 1;                                    
                                     RemoveLabel = true; 
                                     break;
                                }
                            }
                        }
                    }

                    if (RemoveLabel)
                    {
                        break;
                    }
                }
                #endregion

                #region 回溯获得两个Cluster
                List<int> pAreaNumber = new List<int>();
                for (int j = 0; j < FeatureNum; j++)
                {
                    pAreaNumber.Add(j);
                }
                AreaCluster.Clear();

                for (int k = 0; k <ClusterSum; k++)
                {
                    List<int> pCluster = new List<int>();
                    List<int> fCluster = new List<int>();
                    pCluster.Add(pAreaNumber[0]); //任取一个节点作为首节点，遍历树
                    fCluster.Add(pAreaNumber[0]);
                    pAreaNumber.RemoveAt(0);

                    do
                    {
                        int pNumber = pCluster[0]; pCluster.RemoveAt(0);

                        for (int m = 0; m < EdgesGroup.Count; m++)
                        {
                            List<int> pEdge = EdgesGroup[m];

                            if (pNumber == pEdge[0])
                            {
                                if (!fCluster.Contains(pEdge[1]))
                                {
                                    pCluster.Add(pEdge[1]);
                                    fCluster.Add(pEdge[1]);
                                    pAreaNumber.Remove(pEdge[1]);
                                }
                            }

                            else if (pNumber == pEdge[1])
                            {
                                if (!fCluster.Contains(pEdge[0]))
                                {
                                    pCluster.Add(pEdge[0]);
                                    fCluster.Add(pEdge[0]);
                                    pAreaNumber.Remove(pEdge[0]);
                                }
                            }
                        }
                    } while (pCluster.Count > 0);

                    AreaCluster.Add(fCluster);
                }
                #endregion

                #region 判断当前cluster是否满足约束条件
                if (this.checkBox1.Checked)
                {
                    bool StopLabel = false;
                    for (int n = 0; n < AreaCluster.Count; n++)
                    {
                        if (AreaCluster[n].Count > AreaNumber)
                        {
                            StopLabel = true;
                        }
                    }

                    if (!StopLabel)
                    {
                        break;
                    }
                }

                if (this.checkBox2.Checked)
                {
                    if (AreaCluster.Count >= MaxNumber)
                    {
                        break;
                    }
                }
                #endregion
            }
            #endregion
            #endregion

            #region 将建筑物团分类要求添加到字段中
            function.AddField(pFeatureLayer.FeatureClass, "CluLab1", esriFieldType.esriFieldTypeInteger);
            for (int i = 0; i < AreaCluster.Count; i++)
            {
                List<int> kCluster = AreaCluster[i];
                for (int j = 0; j < kCluster.Count; j++)
                {
                    IFeature kFeature = pFeatureLayer.FeatureClass.GetFeature(kCluster[j]);
                    IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
                    IWorkspace workspace = dataset.Workspace;
                    IWorkspaceEdit wse = workspace as IWorkspaceEdit;

                    IFields pFields = kFeature.Fields;
                    wse.StartEditing(false);
                    wse.StartEditOperation();

                    int fnum;
                    fnum = pFields.FieldCount;

                    for (int m = 0; m < fnum; m++)
                    {
                        if (pFields.get_Field(m).Name == "CluLab1")
                        {
                            int field1 = pFields.FindField("CluLab1");
                            kFeature.set_Value(field1, i);
                            kFeature.Store();
                        }
                    }

                    wse.StopEditOperation();
                    wse.StopEditing(true);
                }
            }
            #endregion
        }
        #endregion

        #region 裁剪方法3
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
            pFeatureLayer = function.GetLayer(pMap, this.comboBox1.Text.ToString());
            if (pFeatureLayer.FeatureClass.FeatureCount(null) == 0)
            {
                MessageBox.Show("要素集为空");
                return;
            }

            int AreaNumber = int.Parse(this.comboBox2.Text.ToString());
            int MaxNumber = int.Parse(this.comboBox4.Text.ToString());
            #endregion

            #region 面积与周长计算计算
            List<double> AreaList = new List<double>();
            List<double> LengthList = new List<double>();
            for (int i = 0; i < pFeatureLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                double Length = pPolygon.Length;
                LengthList.Add(Length);

                IArea pArea = pPolygon as IArea;
                double nArea = pArea.Area;
                AreaList.Add(nArea);
            }
            #endregion

            #region 求面积平均值
            double AreaSum = 0;
            for (int i = 0; i < AreaList.Count; i++)
            {
                AreaSum = AreaSum + AreaList[i];
            }

            double AverageArea = AreaSum / AreaList.Count;
            #endregion

            #region 面积标准差计算
            double SquareSum = 0;
            for (int i = 0; i < AreaList.Count; i++)
            {
                SquareSum = SquareSum + (AreaList[i] - AverageArea) * (AreaList[i] - AverageArea);
            }
            double Std = Math.Sqrt(SquareSum / AreaList.Count);
            #endregion

            #region 邻接关系计算
            int FeatureNum = pFeatureLayer.FeatureClass.FeatureCount(null);
            int[,] matrixGraph = new int[FeatureNum, FeatureNum];

            #region 矩阵初始化(不相邻用0表示)
            for (int i = 0; i < FeatureNum; i++)
            {
                for (int j = 0; j < FeatureNum; j++)
                {
                    matrixGraph[i, j] = 0;
                }
            }
            #endregion

            #region 矩阵赋值(0表示不邻接；1表示相交或邻接；)
            for (int i = 0; i < FeatureNum; i++)
            {
                IFeature pFeature1 = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon1 = pFeature1.Shape as IPolygon;
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (j != i)
                    {
                        IFeature pFeature2 = pFeatureLayer.FeatureClass.GetFeature(j);
                        IPolygon pPolygon2 = pFeature2.Shape as IPolygon;
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry0Dimension);
                        IGeometry iGeo2 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if (!iGeo1.IsEmpty || !iGeo2.IsEmpty)
                        {
                            matrixGraph[i, j] = matrixGraph[j, i] = 1;
                        }
                    }
                }
            }
            #endregion

            #endregion

            #region 根据相交长度对矩阵赋值（不相交是1000000000；邻接的话用1000000000-邻接长度）
            double[,] IntersectLengthMatrix = new double[FeatureNum, FeatureNum];
            for (int i = 0; i < FeatureNum; i++)
            {
                IFeature pFeature1 = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon1 = pFeature1.Shape as IPolygon;
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (matrixGraph[i, j] == 1)
                    {
                        IFeature pFeature2 = pFeatureLayer.FeatureClass.GetFeature(j);
                        IPolygon pPolygon2 = pFeature2.Shape as IPolygon;
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if (!iGeo1.IsEmpty)
                        {
                            IPolyline pPolyline = iGeo1 as IPolyline;
                            IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = 100000000 - pPolyline.Length;
                        }

                    }

                    else
                    {
                        IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = 100000000;
                    }
                }
            }
            #endregion

            List<List<int>> AreaCluster = new List<List<int>>();//存储建筑物团

            #region 区域编号存储
            List<int> AreaNumList=new List<int>();
            for(int i=0;i<FeatureNum;i++)
            {
                AreaNumList.Add(i);
            }
            #endregion

            #region 首先合并视觉邻近区域（即相交长度超过某区域周长的50%，则合并）
            //未来需要改进的地方：1、应该是将视觉最邻近的两个进行合并
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (matrixGraph[AreaNumList[i], AreaNumList[j]] == 1)
                    {
                        if (IntersectLengthMatrix[AreaNumList[i], AreaNumList[j]] > LengthList[AreaNumList[i]] * 0.5 || IntersectLengthMatrix[AreaNumList[i], AreaNumList[j]] > LengthList[AreaNumList[j]] * 0.5)
                        {
                            List<int> pCluster = new List<int>();
                            pCluster.Add(AreaNumList[i]); pCluster.Add(AreaNumList[j]);                           
                            AreaNumList.Remove(pCluster[0]); AreaNumList.Remove(pCluster[1]);
                            AreaCluster.Add(pCluster);

                            break;
                        }
                    }
                }
            }
            #endregion

            #region 将面积小于阈值（平均值-标准差）的区域筛选出来（必须与邻近建筑物合幅；优先与面积超过阈值单独成幅）
            double tsArea=AverageArea-Std;
            List<int> tsAreaNumList = new List<int>();
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                if (AreaList[AreaNumList[i]] < tsArea)
                {
                    tsAreaNumList.Add(AreaNumList[i]);
                }
            }
            #endregion

            #region 将面积超过平均值+标准差面积的区域筛选出来（必须单独成幅）
            double tbArea = AverageArea + Std;
            List<int> tbAreaNumList = new List<int>();
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                if (AreaList[AreaNumList[i]] > tbArea)
                {
                    tbAreaNumList.Add(AreaNumList[i]);
                }
            }
            #endregion

            #region 将面积介于（平均值-标准差到平均值+标准差）的区域筛选出来（可合幅，也可以单独成幅）
            List<int> tmAreaNumList = new List<int>();
            for (int i = 0; i < AreaNumList.Count;i++ )
            {
                if (AreaList[AreaNumList[i]] > tsArea && AreaList[AreaNumList[i]] < tbArea)
                {
                    tmAreaNumList.Add(AreaNumList[i]);
                }
            }
            #endregion

            #region 处理面积小于阈值的区域
            #region 小区域内部两两合并(相接；相接长度大于周长的10%)
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                for (int j = 0; j < tsAreaNumList.Count; j++)
                {
                    if (j != i)
                    {
                        if (matrixGraph[tsAreaNumList[i], tsAreaNumList[j]] == 1)
                        {
                            if (IntersectLengthMatrix[tsAreaNumList[i], tsAreaNumList[j]] > LengthList[tsAreaNumList[i]] * 0.1 || IntersectLengthMatrix[tsAreaNumList[i], tsAreaNumList[j]] > LengthList[tsAreaNumList[j]] * 0.1)
                            {
                                List<int> pCluster = new List<int>();
                                pCluster.Add(tsAreaNumList[i]); pCluster.Add(tsAreaNumList[j]);
                                tsAreaNumList.Remove(pCluster[0]); tmAreaNumList.Remove(pCluster[1]);
                                AreaCluster.Add(pCluster);
                                break;
                            }
                        }
                    }
                }
            }
            #endregion

            #region 对于无法合并的小个体，在中等区域中寻找合并个体（相接；相接长度大于周长的10%）
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                for (int j = 0; j < tmAreaNumList.Count; j++)
                {
                    if (matrixGraph[tsAreaNumList[i], tmAreaNumList[j]] == 1)
                    {
                        if (IntersectLengthMatrix[tsAreaNumList[i], tmAreaNumList[j]] > LengthList[tsAreaNumList[i]] * 0.1 || IntersectLengthMatrix[tsAreaNumList[i], tmAreaNumList[j]] > LengthList[tmAreaNumList[j]] * 0.1)
                        {
                            List<int> pCluster = new List<int>();
                            pCluster.Add(tsAreaNumList[i]); pCluster.Add(tmAreaNumList[j]);
                            tsAreaNumList.Remove(pCluster[0]);tmAreaNumList.Remove(pCluster[1]);
                            AreaCluster.Add(pCluster);

                            break;
                        }
                    }
                }
            }
            #endregion

            #region 对于无法合并的小个体，在高等级区域中寻找个体
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                for (int j = 0; j < tbAreaNumList.Count; j++)
                {
                    if (matrixGraph[tsAreaNumList[i], tbAreaNumList[j]] == 1)
                    {
                        if (IntersectLengthMatrix[tsAreaNumList[i], tbAreaNumList[j]] > LengthList[tsAreaNumList[i]] * 0.1 || IntersectLengthMatrix[tsAreaNumList[i], tbAreaNumList[j]] > LengthList[tbAreaNumList[j]] * 0.1)
                        {
                            List<int> pCluster = new List<int>();
                            pCluster.Add(tsAreaNumList[i]); pCluster.Add(tbAreaNumList[j]);
                            tsAreaNumList.Remove(pCluster[0]); tbAreaNumList.Remove(pCluster[1]);
                            AreaCluster.Add(pCluster);

                            break;
                        }
                    }
                }
            }
            #endregion

            #region 无法找到合并个体，单独成幅
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                List<int> pCluster = new List<int>();
                pCluster.Add(tsAreaNumList[i]);
                AreaCluster.Add(pCluster);
            }
            tsAreaNumList.Clear();
            #endregion

            #endregion

            #region 处理中等面积区域
            #region 对于能合并的则尽量合并
            for (int i = 0; i < tmAreaNumList.Count; i++)
            {
                for (int j = 0; j < tmAreaNumList.Count; j++)
                {
                    if (i != j)
                    {
                        if (matrixGraph[tmAreaNumList[i], tmAreaNumList[j]] == 1)
                        {
                            if (IntersectLengthMatrix[tmAreaNumList[i], tmAreaNumList[j]] > LengthList[tmAreaNumList[i]] * 0.1 || IntersectLengthMatrix[tmAreaNumList[i], tmAreaNumList[j]] > LengthList[tmAreaNumList[j]] * 0.1)
                            {
                                List<int> pCluster = new List<int>();
                                pCluster.Add(tmAreaNumList[i]); pCluster.Add(tmAreaNumList[j]);
                                tmAreaNumList.Remove(pCluster[0]); tmAreaNumList.Remove(pCluster[1]);
                                AreaCluster.Add(pCluster);

                                break;
                            }
                        }
                    }
                }
            }
            #endregion

            #region 对于单独个体，单独成幅
            for (int i = 0; i < tmAreaNumList.Count; i++)
            {
                List<int> pCluster = new List<int>();
                pCluster.Add(tmAreaNumList[i]);
                AreaCluster.Add(pCluster);
            }
            tmAreaNumList.Clear();
            #endregion
            #endregion

            #region 处理大面积区域（剩下的大面积区域，单独成幅）
            for (int i = 0; i < tbAreaNumList.Count; i++)
            {
                List<int> pCluster = new List<int>();
                pCluster.Add(tbAreaNumList[i]);
                AreaCluster.Add(pCluster);
            }

            tbAreaNumList.Clear();
            #endregion

            #region 将建筑物团分类要求添加到字段中
            function.AddField(pFeatureLayer.FeatureClass, "CluLab2", esriFieldType.esriFieldTypeInteger);
            for (int i = 0; i < AreaCluster.Count; i++)
            {
                List<int> kCluster = AreaCluster[i];
                for (int j = 0; j < kCluster.Count; j++)
                {
                    IFeature kFeature = pFeatureLayer.FeatureClass.GetFeature(kCluster[j]);
                    IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
                    IWorkspace workspace = dataset.Workspace;
                    IWorkspaceEdit wse = workspace as IWorkspaceEdit;

                    IFields pFields = kFeature.Fields;
                    wse.StartEditing(false);
                    wse.StartEditOperation();

                    int fnum;
                    fnum = pFields.FieldCount;

                    for (int m = 0; m < fnum; m++)
                    {
                        if (pFields.get_Field(m).Name == "CluLab2")
                        {
                            int field1 = pFields.FindField("CluLab2");
                            kFeature.set_Value(field1, i);
                            kFeature.Store();
                        }
                    }

                    wse.StopEditOperation();
                    wse.StopEditing(true);
                }
            }
            #endregion
        }
        #endregion

        #region 裁剪方法3改进
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
            pFeatureLayer = function.GetLayer(pMap, this.comboBox1.Text.ToString());
            if (pFeatureLayer.FeatureClass.FeatureCount(null) == 0)
            {
                MessageBox.Show("要素集为空");
                return;
            }

            int AreaNumber = int.Parse(this.comboBox2.Text.ToString());
            int MaxNumber = int.Parse(this.comboBox4.Text.ToString());
            #endregion

            #region 面积与周长计算计算
            List<double> AreaList = new List<double>();
            List<double> LengthList = new List<double>();
            for (int i = 0; i < pFeatureLayer.FeatureClass.FeatureCount(null); i++)
            {
                IFeature pFeature = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon = pFeature.Shape as IPolygon;
                double Length = pPolygon.Length;
                LengthList.Add(Length);

                IArea pArea = pPolygon as IArea;
                double nArea = pArea.Area;
                AreaList.Add(nArea);
            }
            #endregion

            #region 求面积平均值
            double AreaSum = 0;
            for (int i = 0; i < AreaList.Count; i++)
            {
                AreaSum = AreaSum + AreaList[i];
            }

            double AverageArea = AreaSum / AreaList.Count;
            #endregion

            #region 面积标准差计算
            double SquareSum = 0;
            for (int i = 0; i < AreaList.Count; i++)
            {
                SquareSum = SquareSum + (AreaList[i] - AverageArea) * (AreaList[i] - AverageArea);
            }
            double Std = Math.Sqrt(SquareSum / AreaList.Count);
            #endregion

            #region 邻接关系计算
            int FeatureNum = pFeatureLayer.FeatureClass.FeatureCount(null);
            int[,] matrixGraph = new int[FeatureNum, FeatureNum];

            #region 矩阵初始化(不相邻用0表示)
            for (int i = 0; i < FeatureNum; i++)
            {
                for (int j = 0; j < FeatureNum; j++)
                {
                    matrixGraph[i, j] = 0;
                }
            }
            #endregion

            #region 矩阵赋值(0表示不邻接；1表示相交或邻接；)
            for (int i = 0; i < FeatureNum; i++)
            {
                IFeature pFeature1 = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon1 = pFeature1.Shape as IPolygon;
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (j != i)
                    {
                        IFeature pFeature2 = pFeatureLayer.FeatureClass.GetFeature(j);
                        IPolygon pPolygon2 = pFeature2.Shape as IPolygon;
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry0Dimension);
                        IGeometry iGeo2 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if (!iGeo1.IsEmpty || !iGeo2.IsEmpty)
                        {
                            matrixGraph[i, j] = matrixGraph[j, i] = 1;
                        }
                    }
                }
            }
            #endregion

            #endregion

            #region 根据相交长度对矩阵赋值（不相交是1000000000；邻接的话用1000000000-邻接长度）
            double[,] IntersectLengthMatrix = new double[FeatureNum, FeatureNum];
            for (int i = 0; i < FeatureNum; i++)
            {
                IFeature pFeature1 = pFeatureLayer.FeatureClass.GetFeature(i);
                IPolygon pPolygon1 = pFeature1.Shape as IPolygon;
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (matrixGraph[i, j] == 1)
                    {
                        IFeature pFeature2 = pFeatureLayer.FeatureClass.GetFeature(j);
                        IPolygon pPolygon2 = pFeature2.Shape as IPolygon;
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if (!iGeo1.IsEmpty)
                        {
                            IPolyline pPolyline = iGeo1 as IPolyline;
                            IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = 100000000 - pPolyline.Length;
                        }

                    }

                    else
                    {
                        IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = 100000000;
                    }
                }
            }
            #endregion

            List<List<int>> AreaCluster = new List<List<int>>();//存储建筑物团

            #region 区域编号存储
            List<int> AreaNumList = new List<int>();
            for (int i = 0; i < FeatureNum; i++)
            {
                AreaNumList.Add(i);
            }
            #endregion

            #region 首先合并视觉邻近区域（即相交长度超过某区域周长的50%，则合并）
            //未来需要改进的地方：1、应该是将视觉最邻近的两个进行合并
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (matrixGraph[AreaNumList[i], AreaNumList[j]] == 1)
                    {
                        if (IntersectLengthMatrix[AreaNumList[i], AreaNumList[j]] > LengthList[AreaNumList[i]] * 0.5 || IntersectLengthMatrix[AreaNumList[i], AreaNumList[j]] > LengthList[AreaNumList[j]] * 0.5)
                        {
                            List<int> pCluster = new List<int>();
                            pCluster.Add(AreaNumList[i]); pCluster.Add(AreaNumList[j]);
                            AreaNumList.Remove(pCluster[0]); AreaNumList.Remove(pCluster[1]);
                            AreaCluster.Add(pCluster);

                            break;
                        }
                    }
                }
            }
            #endregion

            #region 将面积小于阈值（平均值-标准差）的区域筛选出来（必须与邻近建筑物合幅；优先与面积超过阈值单独成幅）
            double tsArea = AverageArea - Std;
            List<int> tsAreaNumList = new List<int>();
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                if (AreaList[AreaNumList[i]] < tsArea)
                {
                    tsAreaNumList.Add(AreaNumList[i]);
                }
            }
            #endregion

            #region 将面积超过平均值+标准差面积的区域筛选出来（必须单独成幅）
            double tbArea = AverageArea + Std;
            List<int> tbAreaNumList = new List<int>();
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                if (AreaList[AreaNumList[i]] > tbArea)
                {
                    tbAreaNumList.Add(AreaNumList[i]);
                }
            }
            #endregion

            #region 将面积介于（平均值-标准差到平均值+标准差）的区域筛选出来（可合幅，也可以单独成幅）
            List<int> tmAreaNumList = new List<int>();
            for (int i = 0; i < AreaNumList.Count; i++)
            {
                if (AreaList[AreaNumList[i]] > tsArea && AreaList[AreaNumList[i]] < tbArea)
                {
                    tmAreaNumList.Add(AreaNumList[i]);
                }
            }
            #endregion

            #region 处理面积小于阈值的区域
            #region 小区域内部两两合并(相接；相接长度大于周长的10%)
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                List<int> kCluster = new List<int>();
                for (int j = 0; j < tsAreaNumList.Count; j++)
                {
                    if (j != i)
                    {
                        if (matrixGraph[tsAreaNumList[i], tsAreaNumList[j]] == 1)
                        {
                            kCluster.Add(tsAreaNumList[j]);                           
                        }
                    }
                }

                double maxIntersectLength = 0;
                int AreaLabel = -1;
                for (int j = 0; j < kCluster.Count; j++)
                {
                    if (IntersectLengthMatrix[tsAreaNumList[i], kCluster[j]] > maxIntersectLength)
                    {
                        maxIntersectLength=IntersectLengthMatrix[tsAreaNumList[i], kCluster[j]];
                        AreaLabel = kCluster[j];
                    }
                }

                if (maxIntersectLength != 0)
                {
                    if (maxIntersectLength > LengthList[tsAreaNumList[i]] * 0.1 || maxIntersectLength > LengthList[AreaLabel] * 0.1)
                    {
                        List<int> pCluster = new List<int>();
                        pCluster.Add(tsAreaNumList[i]); pCluster.Add(AreaLabel);
                        tsAreaNumList.Remove(pCluster[0]); tsAreaNumList.Remove(pCluster[1]);
                        AreaCluster.Add(pCluster);
                    }
                }
            }
            #endregion

            #region 对于无法合并的小个体，在中等区域中寻找合并个体（相接；相接长度大于周长的10%）
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                List<int> kCluster = new List<int>();
                for (int j = 0; j < tmAreaNumList.Count; j++)
                {                  
                    if (matrixGraph[tsAreaNumList[i], tmAreaNumList[j]] == 1)
                    {
                        kCluster.Add(tmAreaNumList[j]);  
                    }                    
                }

                double maxIntersectLength = 0;
                int AreaLabel = -1;
                for (int j = 0; j < kCluster.Count; j++)
                {
                    if (IntersectLengthMatrix[tsAreaNumList[i], kCluster[j]] > maxIntersectLength)
                    {
                        maxIntersectLength = IntersectLengthMatrix[tsAreaNumList[i], kCluster[j]];
                        AreaLabel = kCluster[j];
                    }
                }

                if (maxIntersectLength != 0)
                {
                    if (maxIntersectLength > LengthList[tsAreaNumList[i]] * 0.1 || maxIntersectLength > LengthList[AreaLabel] * 0.1)
                    {
                        List<int> pCluster = new List<int>();
                        pCluster.Add(tsAreaNumList[i]); pCluster.Add(AreaLabel);
                        tsAreaNumList.Remove(pCluster[0]); tmAreaNumList.Remove(pCluster[1]);
                        AreaCluster.Add(pCluster);
                    }
                }
            }
            #endregion

            #region 对于无法合并的小个体，在高等级区域中寻找个体
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                List<int> kCluster = new List<int>();
                for (int j = 0; j < tbAreaNumList.Count; j++)
                {
                    if (matrixGraph[tsAreaNumList[i], tbAreaNumList[j]] == 1)
                    {
                        kCluster.Add(tbAreaNumList[j]);  
                    }
                }

                double maxIntersectLength = 0;
                int AreaLabel = -1;
                for (int j = 0; j < kCluster.Count; j++)
                {
                    if (IntersectLengthMatrix[tsAreaNumList[i], kCluster[j]] > maxIntersectLength)
                    {
                        maxIntersectLength = IntersectLengthMatrix[tsAreaNumList[i], kCluster[j]];
                        AreaLabel = kCluster[j];
                    }
                }

                if (maxIntersectLength != 0)
                {
                    if (maxIntersectLength > LengthList[tsAreaNumList[i]] * 0.1 || maxIntersectLength > LengthList[AreaLabel] * 0.1)
                    {
                        List<int> pCluster = new List<int>();
                        pCluster.Add(tsAreaNumList[i]); pCluster.Add(AreaLabel);
                        tsAreaNumList.Remove(pCluster[0]); tbAreaNumList.Remove(pCluster[1]);
                        AreaCluster.Add(pCluster);
                    }
                }
            }
            #endregion

            #region 无法找到合并个体，单独成幅
            for (int i = 0; i < tsAreaNumList.Count; i++)
            {
                List<int> pCluster = new List<int>();
                pCluster.Add(tsAreaNumList[i]);
                AreaCluster.Add(pCluster);
            }
            tsAreaNumList.Clear();
            #endregion

            #endregion

            #region 处理中等面积区域
            #region 对于能合并的则尽量合并
            for (int i = 0; i < tmAreaNumList.Count; i++)
            {
                List<int> kCluster = new List<int>();
                for (int j = 0; j < tmAreaNumList.Count; j++)
                {
                    if (i != j)
                    {
                        if (matrixGraph[tmAreaNumList[i], tmAreaNumList[j]] == 1)
                        {
                            kCluster.Add(tmAreaNumList[j]); 
                        }
                    }
                }

                double maxIntersectLength = 0;
                int AreaLabel = -1;
                for (int j = 0; j < kCluster.Count; j++)
                {
                    if (IntersectLengthMatrix[tmAreaNumList[i], kCluster[j]] > maxIntersectLength)
                    {
                        maxIntersectLength = IntersectLengthMatrix[tmAreaNumList[i], kCluster[j]];
                        AreaLabel = kCluster[j];
                    }
                }

                if (maxIntersectLength != 0)
                {
                    if (maxIntersectLength > LengthList[tmAreaNumList[i]] * 0.1 || maxIntersectLength > LengthList[AreaLabel] * 0.1)
                    {
                        List<int> pCluster = new List<int>();
                        pCluster.Add(tmAreaNumList[i]); pCluster.Add(AreaLabel);
                        tmAreaNumList.Remove(pCluster[0]); tmAreaNumList.Remove(pCluster[1]);
                        AreaCluster.Add(pCluster);
                    }
                }
            }
            #endregion

            #region 对于单独个体，单独成幅
            for (int i = 0; i < tmAreaNumList.Count; i++)
            {
                List<int> pCluster = new List<int>();
                pCluster.Add(tmAreaNumList[i]);
                AreaCluster.Add(pCluster);
            }
            tmAreaNumList.Clear();
            #endregion
            #endregion

            #region 处理大面积区域（剩下的大面积区域，单独成幅）
            for (int i = 0; i < tbAreaNumList.Count; i++)
            {
                List<int> pCluster = new List<int>();
                pCluster.Add(tbAreaNumList[i]);
                AreaCluster.Add(pCluster);
            }

            tbAreaNumList.Clear();
            #endregion

            #region 将建筑物团分类要求添加到字段中
            function.AddField(pFeatureLayer.FeatureClass, "CluLab3", esriFieldType.esriFieldTypeInteger);
            for (int i = 0; i < AreaCluster.Count; i++)
            {
                List<int> kCluster = AreaCluster[i];
                for (int j = 0; j < kCluster.Count; j++)
                {
                    IFeature kFeature = pFeatureLayer.FeatureClass.GetFeature(kCluster[j]);
                    IDataset dataset = pFeatureLayer.FeatureClass as IDataset;
                    IWorkspace workspace = dataset.Workspace;
                    IWorkspaceEdit wse = workspace as IWorkspaceEdit;

                    IFields pFields = kFeature.Fields;
                    wse.StartEditing(false);
                    wse.StartEditOperation();

                    int fnum;
                    fnum = pFields.FieldCount;

                    for (int m = 0; m < fnum; m++)
                    {
                        if (pFields.get_Field(m).Name == "CluLab3")
                        {
                            int field1 = pFields.FindField("CluLab3");
                            kFeature.set_Value(field1, i);
                            kFeature.Store();
                        }
                    }

                    wse.StopEditOperation();
                    wse.StopEditing(true);
                }
            }
            #endregion
        }
        #endregion
    }
}