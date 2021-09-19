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
        /// Compute TimeLocalMoranI
        /// </summary>
        /// <param name="TimeValue"></param>
        /// <param name="TargetTime"></param>
        /// <returns></returns>
        public double TimeLocalMoranI(Dictionary<int, double> TimeValue, int TargetTime)
        {
            double LM = 0;

            #region Get AveValue and WeigthMatrix
            List<double> ValueList = TimeValue.Values.ToList();
            double AveValue = this.AveCompute(ValueList);
            List<int> TimeList = TimeValue.Keys.ToList();
            List<double> WeigthMatrix = this.GetTimeWeigth(ValueList, TargetTime);
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
        /// <returns></returns>
        public List<double> TimeLocalMoranIList(Dictionary<int, double> TimeValue)
        {
            List<int> TimeList = TimeValue.Keys.ToList();
            List<double> TimeLocalMoranIList = new List<double>();

            for (int i = 0; i < TimeList.Count; i++)
            {
                double LocalMoralI = this.TimeLocalMoranI(TimeValue, i);
                TimeLocalMoranIList.Add(LocalMoralI);
            }

            return TimeLocalMoranIList;
        }

        /// <summary>
        /// Compute TimeGlobalMoranI
        /// </summary>
        /// <param name="TimeValue"></param>
        /// <returns></returns>
        public double GlobalMoranI(Dictionary<int, double> TimeValue)
        {
            double GlobalMoranI = 0;

            #region Computation
            List<int> TimeList = TimeValue.Keys.ToList();
            double Ave=this.AveCompute(TimeValue.Values.ToList());//Ave value

            double S = 0; double Z = 0;
            for (int i = 0; i < TimeList.Count; i++)
            {
                List<double> TimeWeight = this.GetTimeWeigth(TimeValue.Values.ToList(), i);//Get TimeWeight
                for (int j = 0; j < TimeList.Count; j++)
                {
                    Z = TimeWeight[j] * (TimeValue[i] - Ave) * (TimeValue[j] - Ave);
                }

                S = (TimeValue[i] - Ave) * (TimeValue[i] - Ave);
            }
            #endregion

            GlobalMoranI = Z / S;

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

            #region TimeIndex=0
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

            #region TimeIndex=1
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

            #region TimeIndex=ValueList.count-1
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

            #region TimeIndex=ValueList.count-2
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
        /// Compute the class Entroy
        /// </summary>
        /// <param name="FreList"></param>frequency for each class
        /// <returns></returns>
        public double ClassEntroy(List<double> FreList)
        {
            double CEntroy = 0;

            for (int i = 0; i < FreList.Count; i++)
            {
                CEntroy = CEntroy - FreList[i] * Math.Log(2, FreList[i]);
            }

            return CEntroy;
        }

        /// <summary>
        /// Compute the metric entroy of a map (ClassEntroy)
        /// </summary>
        /// <param name="PolygonValue"></param>
        /// <returns></returns>
        public double MapMetricEntroy1(Dictionary<IPolygon, int> PolygonValue)
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
                MMEntroy = MMEntroy - SumClassArea / SumArea * Math.Log(2, SumClassArea / SumArea);
            }
            #endregion

            return MMEntroy;
        }


        /// <summary>
        /// Compute the metric entroy of a map (UnitClassEntroy)
        /// </summary>
        /// <param name="PolygonValue"></param>
        /// <returns></returns>
        public double MapMetricEntroy2(Dictionary<IPolygon, int> PolygonValue)
        {
            double MMEntroy = 0;

            #region MapMetricEntroy
            double SumArea = this.GetSumArea(PolygonValue.Keys.ToList());


            foreach (KeyValuePair<IPolygon, int> kv in PolygonValue)
            {
                IPolygon pPolygon = kv.Key;
                IArea pArea = pPolygon as IArea;
                double Area = pArea.Area;
                MMEntroy = MMEntroy - Area / SumArea * Math.Log(2, Area / SumArea);
            }

            #endregion

            return MMEntroy;
        }

        /// <summary>
        /// Compute the Thematic Entroy of a map
        /// </summary>
        /// <param name="PolygonValue"></param>
        /// <returns></returns>
        public double MapThematicEntroy(Dictionary<IPolygon, int> PolygonValue)
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
                    MTEntroy = MTEntroy - ckv.Value / TouchCount * Math.Log(2, ckv.Value / TouchCount);
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
