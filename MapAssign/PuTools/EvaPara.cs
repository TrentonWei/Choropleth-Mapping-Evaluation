using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;

namespace MapAssign.PuTools
{
    class EvaPara
    {
        /// <summary>
        /// Compute SpatialLocalMoranI
        /// </summary>
        /// <param name="PolygonValue">Polygons and their values</param>
        /// <param name="TargetPo"></param>
        /// <returns></returns>
        public double SpatialLocalMoranI(Dictionary<IPolygon, double> PolygonValue, IPolygon TargetPo)
        {
            double LM = 0;

            #region Get AveValue and WeigthMatrix
            List<double> ValueList = PolygonValue.Values.ToList();
            double AveValue = this.AveCompute(ValueList);
            List<IPolygon> PoList=PolygonValue.Keys.ToList();
            List<double> WeigthMatrix=this.GetLinearWeigth(PoList,TargetPo);
            #endregion

            #region LM Compute
            double S = 0;
            double Z = 0;

            foreach (KeyValuePair<IPolygon, double> Kv in PolygonValue)
            {
                S = S + (Kv.Value - AveValue) * (Kv.Value - AveValue);
                Z = Z + WeigthMatrix[PoList.IndexOf(Kv.Key)] * (Kv.Value - AveValue);
            }

            LM = (PolygonValue[TargetPo] - AveValue) * Z * ValueList.Count / S;
            #endregion

            return LM;
        }

        /// <summary>
        /// Compute TimeLocalMoranI for TargetTime
        /// </summary>
        /// <param name="TimeValue"></param>
        /// <param name="TargetTime"></param>
        /// selfLabel=true 考虑自身权重；selfLabel=false不考虑自身权重
        /// <returns></returns>
        public double TimeLocalMoranI(Dictionary<int, double> TimeValue, int TargetTime,int Type,int T,double w1,double w2,bool selfLabel)
        {
            double LM = 0;
            List<double> ValueList = TimeValue.Values.ToList();
            double AveValue = this.AveCompute(ValueList);
            List<int> TimeList = TimeValue.Keys.ToList();
            List<double> WeigthMatrix = new List<double>();

            #region 权重计算
            if (Type == 1)//普通权重
            {
                WeigthMatrix = this.GetTimeWeigth(TimeValue.Values.ToList(), TargetTime);//Get TimeWeight
            }

            else if (Type == 2)//不考虑周期的高斯权重
            {
                WeigthMatrix = this.GetGasuWeigth(TimeValue.Values.ToList(), TargetTime);
            }

            else if (Type == 3)//考虑周期的高斯权重
            {
                WeigthMatrix = this.GetGasuWeigthConsiderT(TimeValue.Values.ToList(), TargetTime, T, w1, w2,selfLabel);
            }
            #endregion

            #region LM Compute
            double S = 0;
            double Z = 0;

            foreach (KeyValuePair<int, double> Kv in TimeValue)
            {
                S = S + (Kv.Value - AveValue) * (Kv.Value - AveValue);
                Z = Z + WeigthMatrix[Kv.Key] * (Kv.Value - AveValue);
            }

            LM = (TimeValue[TargetTime] - AveValue) * Z * ValueList.Count / S;
            #endregion

            return LM;
        }

        /// <summary>
        /// Compute TimeLocalMoranI series
        /// </summary>
        /// <param name="TimeValue"></param>
        /// <param name="TargetTime"></param>
        /// selfLabel=true 考虑自身权重；selfLabel=false不考虑自身权重
        /// <returns></returns>
        public List<double> TimeLocalMoranIList(Dictionary<int, double> TimeValue,int Type,int T,double w1,double w2,bool selfLabel)
        {
            List<int> TimeList = TimeValue.Keys.ToList();
            List<double> TimeLocalMoranIList = new List<double>();

            for (int i = 0; i < TimeList.Count; i++)
            {
                double LocalMoralI = this.TimeLocalMoranI(TimeValue, i, Type, T, w1, w2,selfLabel);
                TimeLocalMoranIList.Add(LocalMoralI);
            }

            return TimeLocalMoranIList;
        }

        /// <summary>
        /// Compute TimeGlobalMoranI
        /// </summary>
        /// <param name="TimeValue"></param>
        /// Type表示权重计算考虑的情况 1普通权重；2不考虑周期的高斯权重；3考虑周期的高斯权重
        /// selfLabel=true考虑自身权重；selfLabel=false 不考虑自身权重
        /// <returns></returns>
        public double GlobalTimeMoranI(Dictionary<int, double> TimeValue,int Type,int T,double w1,double w2,bool selfLabel)
        {
            double GlobalMoranI = 0;

            #region Computation
            List<int> TimeList = TimeValue.Keys.ToList();
            double Ave=this.AveCompute(TimeValue.Values.ToList());//Ave value
            double WeightSum = 0;

            double S = 0; double Z = 0;
            for (int i = 0; i < TimeList.Count; i++)
            {
                List<double> TimeWeight=new List<double>();
                
                #region 权重计算
                if (Type == 1)
                {
                    TimeWeight = this.GetTimeWeigth(TimeValue.Values.ToList(), i);//Get TimeWeight
                }

                else if (Type == 2)
                {
                    TimeWeight = this.GetGasuWeigth(TimeValue.Values.ToList(), i);
                }

                else if (Type == 3)
                {
                    TimeWeight = this.GetGasuWeigthConsiderT(TimeValue.Values.ToList(), i, T, w1, w2,selfLabel);
                }
                #endregion

                for (int j = 0; j < TimeList.Count; j++)
                {
                    Z = Z + TimeWeight[j] * (TimeValue[i] - Ave) * (TimeValue[j] - Ave);
                    WeightSum = WeightSum + TimeWeight[j];
                }

                S = S + (TimeValue[i] - Ave) * (TimeValue[i] - Ave);
            }
            #endregion

            GlobalMoranI = (Z * TimeList.Count) / (S * WeightSum);

            return GlobalMoranI;
        }

        /// <summary>
        /// compute the average value
        /// </summary>
        /// <returns></returns>
        /// 0=Nodata；Else=average value
        public double AveCompute(List<double> ValueList)
        {
            if (ValueList.Count > 0)
            {
                double AveValue = 0;

                double SumValue = 0;
                foreach (double Value in ValueList)
                {
                    SumValue = SumValue + Value;
                }

                AveValue = SumValue / ValueList.Count;
                return AveValue;
            }

            else
            {
                return -1;
            }
        }

        /// <summary>
        /// compute the touch relation between polygons
        /// </summary>
        /// <param name="PoList"></param>
        /// <returns></returns> 0=NoTouch；1=Touch
        public int[,] GetTouchRelation(List<IPolygon> PoList)
        {
            int FeatureNum = PoList.Count;
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
                IPolygon pPolygon1 = PoList[i];
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (j != i)
                    {
                        IPolygon pPolygon2 = PoList[j];
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

            return matrixGraph;
        }

        /// <summary>
        /// Compute the weight between polygons
        /// </summary>
        /// <param name="PoList"></param>
        /// /// <param name="TargetPo"></param>
        /// <returns></returns>
        public double[,] GetWeigth(List<IPolygon> PoList,IPolygon TargetPo)
        {
            double TargetLength = TargetPo.Length;//Length of TargetPo

            int FeatureNum = PoList.Count;
            double[,] IntersectLengthMatrix = new double[FeatureNum, FeatureNum];
            for (int i = 0; i < FeatureNum; i++)
            {
                IPolygon pPolygon1 = PoList[i];
                ITopologicalOperator pTopo = pPolygon1 as ITopologicalOperator;
                for (int j = 0; j < FeatureNum; j++)
                {
                    if (i != j)
                    {
                        IPolygon pPolygon2 = PoList[j];
                        IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                        if (!iGeo1.IsEmpty)
                        {
                            IPolyline pPolyline = iGeo1 as IPolyline;
                            IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = pPolyline.Length / TargetLength;
                        }

                        else
                        {
                            IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = 0;
                        }

                    }

                    else
                    {
                        IntersectLengthMatrix[i, j] = IntersectLengthMatrix[j, i] = 0;
                    }
                }
            }

            return IntersectLengthMatrix;
        }

        /// <summary>
        /// Compute the weight between polygons
        /// </summary>
        /// <param name="PoList"></param>
        /// /// <param name="TargetPo"></param>
        /// <returns></returns>
        public List<double> GetLinearWeigth(List<IPolygon> PoList, IPolygon TargetPo)
        {
            double TargetLength = TargetPo.Length;//Length of TargetPo
            int FeatureNum = PoList.Count;
            ITopologicalOperator pTopo = TargetPo as ITopologicalOperator;
            List<double> IntersectLengthMatrix = new List<double>();
            for (int i = 0; i < FeatureNum; i++)
            {
                int j = PoList.IndexOf(TargetPo);
                if (i != j)
                {
                    IPolygon pPolygon2 = PoList[i];
                    IGeometry iGeo1 = pTopo.Intersect(pPolygon2, esriGeometryDimension.esriGeometry1Dimension);

                    if (!iGeo1.IsEmpty)
                    {
                        IPolyline pPolyline = iGeo1 as IPolyline;
                        IntersectLengthMatrix.Add(pPolyline.Length / TargetLength);
                    }

                    else
                    {
                        IntersectLengthMatrix.Add(0);
                    }
                }

                else
                {
                    IntersectLengthMatrix.Add(0);
                }
            }
            

            return IntersectLengthMatrix;
        }

        /// <summary>
        /// Compute the weight between two Times
        /// </summary>
        /// <returns></returns>
        public List<double> GetTimeWeigth(List<Double> ValueList, int TimeIndex)
        {
            List<double> TimeWeight = new List<double>();

            #region TimeIndex=0 //考虑了边缘的情况
            if (TimeIndex == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {                 
                    if(i==1)
                    {
                        TimeWeight.Add(0.7);
                    }
                    else if (i == 2)
                    {
                        TimeWeight.Add(0.3);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=1 //考虑了边缘的情况
            else if (TimeIndex == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 0)
                    {
                        TimeWeight.Add(0.41);
                    }
                    else if (i == 2)
                    {
                        TimeWeight.Add(0.41);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.18);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-1 //考虑了边缘的情况
            else if (TimeIndex == ValueList.Count - 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 2)
                    {
                        TimeWeight.Add(0.7);
                    }
                    else if (i == ValueList.Count - 3)
                    {
                        TimeWeight.Add(0.3);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-2 //考虑了边缘的情况
            else if (TimeIndex == ValueList.Count - 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 1)
                    {
                        TimeWeight.Add(0.41);
                    }
                    else if (i == ValueList.Count - 3)
                    {
                        TimeWeight.Add(0.41);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.18);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region Else
            else
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == TimeIndex - 1)
                    {
                        TimeWeight.Add(0.35);
                    }
                    else if (i == TimeIndex - 2)
                    {
                        TimeWeight.Add(0.15);
                    }
                    else if (i == TimeIndex + 1)
                    {
                        TimeWeight.Add(0.35);
                    }
                    else if (i == TimeIndex + 2)
                    {
                        TimeWeight.Add(0.15);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            return TimeWeight;
        }

        /// <summary>
        /// Compute the weight between two Times 高斯权重
        /// </summary>
        /// <returns></returns>
        public List<double> GetGasuWeigth(List<Double> ValueList, int TimeIndex)
        {
            List<double> TimeWeight = new List<double>();

            #region TimeIndex=0
            if (TimeIndex == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 1)
                    {
                        TimeWeight.Add(0.6826);
                    }
                    else if (i == 2)
                    {
                        TimeWeight.Add(0.2718);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.0456);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=1
            else if (TimeIndex == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 0)
                    {
                        TimeWeight.Add(0.6826 / 1.6826);
                    }
                    else if (i == 2)
                    {
                        TimeWeight.Add(0.6826 / 1.6826);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.2718 / 1.6826);
                    }
                    else if(i==4)
                    {
                        TimeWeight.Add(0.0456 / 1.6826);
                    }

                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex==2
            else if (TimeIndex == 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 0)
                    {
                        TimeWeight.Add(0.2718 / 1.9544);
                    }
                    else if (i == 1)
                    {
                        TimeWeight.Add(0.6826 / 1.9544);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.6826 / 1.9544);
                    }
                    else if (i == 4)
                    {
                        TimeWeight.Add(0.2718 / 1.9544);
                    }
                    else if (i == 5)
                    {
                        TimeWeight.Add(0.0456 / 1.9544);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-1
            else if (TimeIndex == ValueList.Count - 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 2)
                    {
                        TimeWeight.Add(0.6826);
                    }
                    else if (i == ValueList.Count - 3)
                    {
                        TimeWeight.Add(0.2718);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.0456);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-2
            else if (TimeIndex == ValueList.Count - 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 1)
                    {
                        TimeWeight.Add(0.6826 / 1.6826);
                    }
                    else if (i == ValueList.Count - 3)
                    {
                        TimeWeight.Add(0.6826 / 1.6826);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.2718 / 1.6826);
                    }
                    else if (i == ValueList.Count - 5)
                    {
                        TimeWeight.Add(0.0456 / 1.6826);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-3
            else if (TimeIndex == ValueList.Count - 3)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 1)
                    {
                        TimeWeight.Add(0.2718 / 1.9544);
                    }
                    if (i == ValueList.Count - 2)
                    {
                        TimeWeight.Add(0.6826 / 1.9544);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.6826 / 1.9544);
                    }
                    else if (i == ValueList.Count - 5)
                    {
                        TimeWeight.Add(0.2718 / 1.9544);
                    }
                    else if (i == ValueList.Count - 6)
                    {
                        TimeWeight.Add(0.0456 / 1.9544);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            #region Else
            else
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == TimeIndex - 1)
                    {
                        TimeWeight.Add(0.6826 / 2);
                    }
                    else if (i == TimeIndex - 2)
                    {
                        TimeWeight.Add(0.2718 / 2);
                    }
                    else if (i == TimeIndex - 3)
                    {
                        TimeWeight.Add(0.0456 / 2);
                    }
                    else if (i == TimeIndex + 1)
                    {
                        TimeWeight.Add(0.6826 / 2);
                    }
                    else if (i == TimeIndex + 2)
                    {
                        TimeWeight.Add(0.2718 / 2);
                    }
                    else if (i == TimeIndex + 3)
                    {
                        TimeWeight.Add(0.0456 / 2);
                    }
                    else
                    {
                        TimeWeight.Add(0);
                    }
                }
            }
            #endregion

            return TimeWeight;
        }

        /// <summary>
        /// Compute the weight between two Times 高斯权重
        /// </summary>
        /// <param name="ValueList">其他时刻</param>
        /// <param name="TimeIndex">给定时刻</param>
        /// <param name="T">周期</param>
        /// <param name="w1">周期内权重</param>
        /// <param name="w2">周期外</param>
        /// selfLabel=true，权重计算考虑自身；selfLable=false，权重计算不考虑自身
        /// <returns></returns>
        public List<double> GetGasuWeigthConsiderT(List<Double> ValueList, int TimeIndex,int T,double w1,double w2,bool selfLabel)
        {
            List<double> TimeWeight = new List<double>();

            #region TimeIndex=0
            if (TimeIndex == 0)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 1)
                    {
                        TimeWeight.Add(0.6826 * w1);
                    }
                    else if (i == 2)
                    {
                        TimeWeight.Add(0.2718 * w1);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.0456 * w1);
                    }
                    else
                    {                    
                        if (Math.Abs(i - TimeIndex) >= T && Math.Abs(i - TimeIndex) % T == 0)
                        {
                            int D = Math.Abs(i - TimeIndex) / T;

                            if (D == 1)
                            {
                                TimeWeight.Add(0.6826 * w2 / 2);
                            }
                            else if (D == 2)
                            {
                                TimeWeight.Add(0.2718 * w2 / 2);
                            }
                            else if (D == 3)
                            {
                                TimeWeight.Add(0.0456 * w2 / 2);
                            }
                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }

                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                }
            }
            #endregion

            #region TimeIndex=1
            else if (TimeIndex == 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 0)
                    {
                        TimeWeight.Add(0.6826 / 1.6826 * w1);
                    }
                    else if (i == 2)
                    {
                        TimeWeight.Add(0.6826 / 1.6826 * w1);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.2718 / 1.6826 * w1);
                    }
                    else if (i == 4)
                    {
                        TimeWeight.Add(0.0456 / 1.6826 * w1);
                    }

                    else
                    {
                        if (Math.Abs(i - TimeIndex) >= T && Math.Abs(i - TimeIndex) % T == 0)
                        {
                            int D = Math.Abs(i - TimeIndex) / T;

                            if (D == 1)
                            {
                                TimeWeight.Add(0.6826 * w2 / 2);
                            }
                            else if (D == 2)
                            {
                                TimeWeight.Add(0.2718 * w2 / 2);
                            }
                            else if (D == 3)
                            {
                                TimeWeight.Add(0.0456 * w2 / 2);
                            }
                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }

                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                }
            }
            #endregion

            #region TimeIndex==2
            else if (TimeIndex == 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == 0)
                    {
                        TimeWeight.Add(0.2718 / 1.9544 * w1);
                    }
                    else if (i == 1)
                    {
                        TimeWeight.Add(0.6826 / 1.9544 * w1);
                    }
                    else if (i == 3)
                    {
                        TimeWeight.Add(0.6826 / 1.9544 * w1);
                    }
                    else if (i == 4)
                    {
                        TimeWeight.Add(0.2718 / 1.9544 * w1);
                    }
                    else if (i == 5)
                    {
                        TimeWeight.Add(0.0456 / 1.9544 * w1);
                    }
                    else
                    {
                        if (Math.Abs(i - TimeIndex) >= T && Math.Abs(i - TimeIndex) % T == 0)
                        {
                            int D = Math.Abs(i - TimeIndex) / T;

                            if (D == 1)
                            {
                                TimeWeight.Add(0.6826 * w2 / 2);
                            }
                            else if (D == 2)
                            {
                                TimeWeight.Add(0.2718 * w2 / 2);
                            }
                            else if (D == 3)
                            {
                                TimeWeight.Add(0.0456 * w2 / 2);
                            }
                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }

                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-1
            else if (TimeIndex == ValueList.Count - 1)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 2)
                    {
                        TimeWeight.Add(0.6826 * w1);
                    }
                    else if (i == ValueList.Count - 3)
                    {
                        TimeWeight.Add(0.2718 * w1);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.0456 * w1);
                    }
                    else
                    {
                        if (Math.Abs(i - TimeIndex) >= T && Math.Abs(i - TimeIndex) % T == 0)
                        {
                            int D = Math.Abs(i - TimeIndex) / T;

                            if (D == 1)
                            {
                                TimeWeight.Add(0.6826 * w2 / 2);
                            }
                            else if (D == 2)
                            {
                                TimeWeight.Add(0.2718 * w2 / 2);
                            }
                            else if (D == 3)
                            {
                                TimeWeight.Add(0.0456 * w2 / 2);
                            }
                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }

                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-2
            else if (TimeIndex == ValueList.Count - 2)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 1)
                    {
                        TimeWeight.Add(0.6826 / 1.6826 * w1);
                    }
                    else if (i == ValueList.Count - 3)
                    {
                        TimeWeight.Add(0.6826 / 1.6826 * w1);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.2718 / 1.6826 * w1);
                    }
                    else if (i == ValueList.Count - 5)
                    {
                        TimeWeight.Add(0.0456 / 1.6826 * w1);
                    }
                    else
                    {
                        if (Math.Abs(i - TimeIndex) >= T && Math.Abs(i - TimeIndex) % T == 0)
                        {
                            int D = Math.Abs(i - TimeIndex) / T;

                            if (D == 1)
                            {
                                TimeWeight.Add(0.6826 * w2 / 2);
                            }
                            else if (D == 2)
                            {
                                TimeWeight.Add(0.2718 * w2 / 2);
                            }
                            else if (D == 3)
                            {
                                TimeWeight.Add(0.0456 * w2 / 2);
                            }
                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }

                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                }
            }
            #endregion

            #region TimeIndex=ValueList.count-3
            else if (TimeIndex == ValueList.Count - 3)
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    if (i == ValueList.Count - 1)
                    {
                        TimeWeight.Add(0.2718 / 1.9544 * w1);
                    }
                    if (i == ValueList.Count - 2)
                    {
                        TimeWeight.Add(0.6826 / 1.9544 * w1);
                    }
                    else if (i == ValueList.Count - 4)
                    {
                        TimeWeight.Add(0.6826 / 1.9544 * w1);
                    }
                    else if (i == ValueList.Count - 5)
                    {
                        TimeWeight.Add(0.2718 / 1.9544 * w1);
                    }
                    else if (i == ValueList.Count - 6)
                    {
                        TimeWeight.Add(0.0456 / 1.9544 * w1);
                    }
                    else
                    {
                        if (Math.Abs(i - TimeIndex) >= T && Math.Abs(i - TimeIndex) % T == 0)
                        {
                            int D = Math.Abs(i - TimeIndex) / T;

                            if (D == 1)
                            {
                                TimeWeight.Add(0.6826 * w2 / 2);
                            }
                            else if (D == 2)
                            {
                                TimeWeight.Add(0.2718 * w2 / 2);
                            }
                            else if (D == 3)
                            {
                                TimeWeight.Add(0.0456 * w2 / 2);
                            }
                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }

                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Else
            else
            {
                for (int i = 0; i < ValueList.Count; i++)
                {
                    #region 获得距离
                    int D = 0;
                    if (Math.Abs(i - TimeIndex) < T)
                    {
                        D = Math.Abs(i - TimeIndex);
                    }
                    else 
                    {
                        if (Math.Abs(i - TimeIndex) % T == 0)
                        {
                            D = Math.Abs(i - TimeIndex) / T;
                        }
                    }
                    #endregion

                    #region 周期内权重
                    if (Math.Abs(i - TimeIndex) < T)
                    {
                        if (D == 1)
                        {
                            TimeWeight.Add(0.6826 * w1 / 2);
                        }
                        else if (D == 2)
                        {
                            TimeWeight.Add(0.2718 * w1 / 2);
                        }
                        else if (D == 3)
                        {
                            TimeWeight.Add(0.0456 * w1 / 2);
                        }
                        else
                        {
                            if (selfLabel)
                            {
                                if (i == TimeIndex)
                                {
                                    TimeWeight.Add(0.5);
                                }

                                else
                                {
                                    TimeWeight.Add(0);
                                }
                            }

                            else
                            {
                                TimeWeight.Add(0);
                            }
                        }
                    }
                    #endregion

                    #region 周期外权重
                    else
                    {
                        if (D == 1)
                        {
                            TimeWeight.Add(0.6826 * w2 / 2);
                        }
                        else if (D == 2)
                        {
                            TimeWeight.Add(0.2718 * w2 / 2);
                        }
                        else if (D == 3)
                        {
                            TimeWeight.Add(0.0456 * w2 / 2);
                        }
                        else
                        {
                            TimeWeight.Add(0);
                        }
                    }
                    #endregion
                }
            }
            #endregion

            return TimeWeight;
        }

        /// <summary>
        /// Compute the class Entroy
        /// </summary>
        /// <param name="FreList"></param>frequency for each class
        /// Type=1 香农熵模型；Type=2 指数熵模型
        /// <returns></returns>
        public double ClassEntroy(List<double> FreList,int Type)
        {
            double CEntroy = 0;
            
            #region 香农熵模型
            if (Type == 1)
            {
                for (int i = 0; i < FreList.Count; i++)
                {
                    if (FreList[i] != 1.0 && FreList[i] != 0)
                    {
                        CEntroy = CEntroy - FreList[i] * Math.Log(2, FreList[i]);
                    }
                }
            }
            #endregion

            #region 指数熵模型
            else if (Type == 2)
            {
                for (int i = 0; i < FreList.Count; i++)
                {
                    CEntroy = CEntroy + FreList[i] * Math.Exp(1 - FreList[i]);
                }
            }
            #endregion

            return CEntroy;
        }

        /// <summary>
        /// Compute the class Entroy
        /// </summary>
        /// <param name="FreList"></param>frequency for each class
        /// Type=1 香农熵模型；Type=2 指数熵模型
        /// <returns></returns>
        public double ClassEntroy(List<int> ClassList,int Type)
        {
            List<int> SingleClass = ClassList.Distinct().ToList();
            List<double> FreList = new List<double>();

            #region GetFreList
            for (int i = 0; i < SingleClass.Count; i++)
            {
                int Count = 0;
                for (int j = 0; j < ClassList.Count; j++)
                {
                    if (ClassList[j] == SingleClass[i])
                    {
                        Count++;
                    }
                }

                double Fre = (double)Count / ClassList.Count;//Int/Int may a Int
                FreList.Add(Fre);
            }
            #endregion

            return this.ClassEntroy(FreList,Type);
        }

        /// <summary>
        /// Compute the metric entroy of a map (ClassEntroy)
        /// 每一类的总面积
        /// </summary>
        /// <param name="PolygonValue"></param>
        /// Type=1 香农熵模型；Type=2 指数熵模型
        /// <returns></returns>
        public double MapMetricEntroy1(Dictionary<IPolygon, int> PolygonValue,int Type)
        {
            double MMEntroy = 0;

            #region MapMetricEntroy
            double SumArea = this.GetSumArea(PolygonValue.Keys.ToList());

            #region GetSingleClass
            List<int> ClassInt = new List<int>();
            foreach (KeyValuePair<IPolygon, int> kv in PolygonValue)
            {
                ClassInt.Add(kv.Value);
            }
            List<int> SingleClass = ClassInt.Distinct().ToList();
            #endregion

            for (int i = 0; i < SingleClass.Count; i++)
            {
                List<IPolygon> ClassPolygon = new List<IPolygon>();
                foreach (KeyValuePair<IPolygon, int> kv in PolygonValue)
                {
                    if (kv.Value == SingleClass[i])
                    {
                        ClassPolygon.Add(kv.Key);
                    }
                }

                double SumClassArea=this.GetSumArea(ClassPolygon);

                #region 香农熵
                if (Type == 1)
                {
                    if (SumClassArea / SumArea != 1 && SumClassArea / SumArea != 0)
                    {
                        MMEntroy = MMEntroy - SumClassArea / SumArea * Math.Log(2, SumClassArea / SumArea);
                    }
                }
                #endregion

                #region 指数熵
                else if (Type == 2)
                {

                    MMEntroy = MMEntroy + SumClassArea / SumArea * Math.Exp(1 - SumClassArea / SumArea);
                }
                #endregion
            }
            #endregion

            return MMEntroy;
        }

        /// <summary>
        /// Compute the metric entroy of a map (UnitClassEntroy)
        /// 每一个区域的面积
        /// </summary>
        /// <param name="PolygonValue"></param>
        /// Type=1 香农熵模型；Type=2 指数熵模型
        /// <returns></returns>
        public double MapMetricEntroy2(Dictionary<IPolygon, int> PolygonValue,int Type)
        {
            double MMEntroy = 0;

            #region MapMetricEntroy
            double SumArea = this.GetSumArea(PolygonValue.Keys.ToList());

            foreach (KeyValuePair<IPolygon, int> kv in PolygonValue)
            {
                IPolygon pPolygon = kv.Key;
                IArea pArea = pPolygon as IArea;
                double Area = pArea.Area;

                #region 香农熵
                if (Type == 1)
                {
                    MMEntroy = MMEntroy - Area / SumArea * Math.Log(2, Area / SumArea);
                }
                #endregion

                #region 指数熵
                else if(Type==2)
                {
                    MMEntroy = MMEntroy + Area / SumArea * Math.Exp(1 - Area / SumArea);
                }
                #endregion
            }

            #endregion

            return MMEntroy;
        }

        /// <summary>
        /// Compute the Thematic Entroy of a map
        /// </summary>
        /// <param name="PolygonValue"></param>
        /// Type=1 香农熵模型；Type=2 指数熵模型
        /// <returns></returns>
        public double MapThematicEntroy(Dictionary<IPolygon, int> PolygonValue,int Type)
        {
            double MTEntroy = 0;

            List<IPolygon> PoList = PolygonValue.Keys.ToList();
            int[,] TouchMatrix = this.GetTouchRelation(PoList);//Compute touch relation 0=NoTouch；1=Touch

            #region GetSingleClass
            List<int> ClassInt = new List<int>();
            foreach (KeyValuePair<IPolygon, int> kv in PolygonValue)
            {
                ClassInt.Add(kv.Value);
            }
            List<int> SingleClass = ClassInt.Distinct().ToList();
            #endregion

            #region MapThematicEntroy

            foreach (KeyValuePair<IPolygon, int> kv in PolygonValue)
            {
                #region GetTouchPolygons
                int PolygonID = PoList.IndexOf(kv.Key);
                List<IPolygon> TouchPolygons = new List<IPolygon>();
                for (int j = 0; j < PoList.Count; j++)
                {
                    if (TouchMatrix[PolygonID, j] == 1)
                    {
                        TouchPolygons.Add(PoList[j]);
                    }
                }
                #endregion

                Dictionary<int, int> ClassCount = new Dictionary<int, int>();//ClassCount around the target polygon
                int TouchCount = TouchPolygons.Count;//Touch Polygon Count

                #region ClassCount around the target polygon
                for (int j = 0; j < SingleClass.Count; j++)
                {
                    int Count = 0;
                    for (int k = 0; k < TouchPolygons.Count; k++)
                    {
                        if (PolygonValue[TouchPolygons[k]] == j)
                        {
                            Count++;
                        }
                    }

                    ClassCount.Add(j, Count);
                }
                #endregion

                #region Computation
                foreach (KeyValuePair<int, int> ckv in ClassCount)
                {
                    double Fre = (double)ckv.Value / TouchCount;//Int/Int may a Int

                    #region 香农熵
                    if (Type == 1)
                    {
                        if (Fre != 1.0 && Fre != 0)
                        {
                            MTEntroy = MTEntroy - Fre * Math.Log(2, Fre);
                        }
                    }
                    #endregion

                    #region
                    else if (Type == 2)
                    {
                        MTEntroy = MTEntroy + Fre * Math.Exp(1 - Fre);
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            return MTEntroy;
        }

        /// <summary>
        /// Compute the sum of Area
        /// </summary>
        /// <param name="PolygonList"></param>
        /// <returns></returns>
        public double GetSumArea(List<IPolygon> PolygonList)
        {
            double SumArea = 0;

            for (int i = 0; i < PolygonList.Count; i++)
            {
                IArea pArea = PolygonList[i] as IArea;
                SumArea = SumArea + pArea.Area;
            }

            return SumArea;
        }
    }
}