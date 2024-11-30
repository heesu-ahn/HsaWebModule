using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static HsaWebModule.ItemExportManager;

namespace HsaWebModule
{
    public class ParameterCollection
    {
        private List<Tuple<string, object>> kvp;
        public ParameterCollection()
        {
            kvp = new List<Tuple<string, object>>();
        }
        public object[] Values
        {
            get
            {
                object[] result = new object[kvp.Count];
                int i = 0;
                kvp.ForEach(k => {; result[i] = k; i++; });
                return result;
            }
        }
        public void Add(string key, object value)
        {
            try
            {
                if (kvp.Count > 0)
                {
                    kvp.RemoveAll((k) => k.Item1.Equals(key));
                }
                kvp.Add(new Tuple<string, object>(key, value));
                kvp.Sort(new Comparison<Tuple<string, object>>((s1, s2) => s1.Item1.CompareTo(s2.Item1)));
            }
            catch (Exception ex)
            {
                Program.WriteLog(ex,true);
            }
        }
    }
    public class ItemExportManager
    {

        public ParameterCollection paramCol;
        public object result;
        public void CallMethod(string methodName) // 동적 함수 호출
        {
            try
            {
                var method = typeof(CreateItem).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    var value = method.Invoke(null, new object[] { paramCol.Values }); // 함수에 변수 전달
                    if (value != null) result = value;
                }
            }
            catch (Exception ex)
            {
                Program.WriteLog(ex,true);
            }
        }
        [Serializable]
        public class Obj
        {
            public string id = "";
            public string[] propertyAr = { "", "", "", "" };
            public string[,] tempMergeRowCell = null;
            public List<int> listInt = CreateItem.LstInt;
            public List<float> tempTableRowHeight = CreateItem.LstFloat;
            public List<string> mergeCells = CreateItem.LstStr;
            public List<object> cells = CreateItem.LstObj;
            public List<List<string>> mergeRowCells = CreateItem.LstInLstStr;
            public Dictionary<string, int> groupBandCntMap = CreateItem.mapInt;
            public Dictionary<string, string> pageProperty = CreateItem.mapStr;
            public Dictionary<string, object> pageMap = CreateItem.mapObj;
            public Dictionary<string, Dictionary<string, object>> pageItemMap = CreateItem.mapInMapObj;
            public Dictionary<string, List<Dictionary<string, object>>> data = CreateItem.mapInListMapObj;
            public int backgRoundColorInt = 0; 
            public float backgRoundAlpha = 0;
            public object pageParserClass;
            public object[] GetParam;

            public Obj()
            {

            }

            public Obj(object[] parameters)
            {
                GetParam = parameters;
                PropertyMappingFnBool(parameters, ref CreateItem.booleanParam, GetVariableName(() => CreateItem.booleanParam));
                PropertyMappingFnStr(parameters, ref CreateItem.strParam, GetVariableName(() => CreateItem.strParam));
                
                object preCheck = null;
                preCheck = (parameters.Where(s => ((Tuple<string, object>)s).Item1 == GetVariableName(() => pageItemMap)).FirstOrDefault());
                if (preCheck != null) pageItemMap = (Dictionary<string, Dictionary<string, object>>)(((Tuple<string, object>)preCheck).Item2);
                preCheck = (parameters.Where(s => ((Tuple<string, object>)s).Item1 == GetVariableName(() => groupBandCntMap)).FirstOrDefault());
                if (preCheck != null) groupBandCntMap = (Dictionary<string, int>)(((Tuple<string, object>)preCheck).Item2);
                preCheck = (parameters.Where(s => ((Tuple<string, object>)s).Item1 == GetVariableName(() => pageMap)).FirstOrDefault());
                if (preCheck != null) pageMap = (Dictionary<string, object>)(((Tuple<string, object>)preCheck).Item2);

            }
            #region 외부 선언 로직
            public void InitGlobalVariable(object[] parameters)
            {
                SetGlobalSetValVariables(parameters); // 전역 변수 현 메소드에서 처리 가능
            }
            public List<Dictionary<string, object>> ConvertItem(object[] parameters)
            {
                Obj obj = new Obj(parameters);
                return ItemsClone(obj); // 지역 변수 (메소드 지역변수이므로 지역변수 내에서 처리 해야 한다)
            }
            #endregion

            #region 내부 처리 로직
            private void ChangeValue(ref object[] parameters, string key, object value) // delete & insert
            {
                parameters = parameters.Where(w => !((Tuple<string, object>)w).Item1.Equals(key)).ToArray();
                List<Tuple<string, object>> kvp = new List<Tuple<string, object>>();
                foreach (Tuple<string, object> item in parameters)
                {
                    kvp.Add(item);
                }
                kvp.Add(new Tuple<string, object>(key, value));
                kvp.Sort(new Comparison<Tuple<string, object>>((s1, s2) => s1.Item1.CompareTo(s2.Item1)));
                object[] result = new object[kvp.Count];
                int i = 0;
                kvp.ForEach(k => { result[i] = k; i++; });
                parameters = result;
            }

            private string GetVariableName<T>(Expression<Func<T>> variableAccessExpression)
            {
                var memberExpression = variableAccessExpression.Body as MemberExpression;
                return memberExpression.Member.Name;
            }
            
            public delegate void BSetter(ref bool lv, object obj);
            private void PropertyMappingFnBool(object[] parameters, ref bool boolParam, string parameterName)
            {
                BSetter setBool = new BSetter(SetBValue);
                if (parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault() != null)
                {
                    var data = parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault();
                    setBool(ref boolParam, ((Tuple<string, object>)data).Item2);
                }
            }
            public delegate void DSetter(ref decimal lv, object obj);
            private void PropertyMappingFnDecimal(object[] parameters, ref decimal decimalParam, string parameterName)
            {
                DSetter setDecimal = new DSetter(SetDValue);
                if (parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault() != null)
                {
                    var data = parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault();
                    setDecimal(ref decimalParam, ((Tuple<string, object>)data).Item2);
                }
            }
            public delegate void FSetter(ref float lv, object obj);
            private void PropertyMappingFnFloat(object[] parameters, ref float floatParam, string parameterName)
            {
                FSetter setFloat = new FSetter(SetFValue);
                if (parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault() != null)
                {
                    var data = parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault();
                    setFloat(ref floatParam, ((Tuple<string, object>)data).Item2);
                }
            }
            public delegate void ISetter(ref int lv, object obj);
            private void PropertyMappingFnInt(object[] parameters, ref int intParam, string parameterName)
            {
                ISetter setInt = new ISetter(SetIValue);
                if (parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault() != null)
                {
                    var data = parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault();
                    setInt(ref intParam, ((Tuple<string, object>)data).Item2);
                }
            }
            public void PropertyMappingFnObj(object[] parameters, ref object listParam, string objName)
            {
                if (parameters.Where(s => ((Tuple<string, object>)s).Item1 == objName).FirstOrDefault() != null)
                {
                    var data = parameters.Where(s => ((Tuple<string, object>)s).Item1 == objName).FirstOrDefault();
                    if (data != null)
                    {
                        data = ((Tuple<string, object>)data).Item2;
                        switch (objName)
                        {
                            case "xArr":
                                CreateItem.LstInt = (List<int>)data;
                                break;
                            case "item":
                                CreateItem.LstObj = (List<object>)data;
                                break;
                            case "TabIndexList":
                                CreateItem.LstStr = (List<string>)data;
                                break;
                            case "groupData":
                                CreateItem.LstMapObj = (List<Dictionary<string, object>>)data;
                                break;
                            case "groupColumn":
                                CreateItem.LstMapStr = (List<Dictionary<string, string>>)data;
                                break;
                        }
                    }

                }
            }
            public delegate void Setter(ref string lv, object obj);
            private void PropertyMappingFnStr(object[] parameters, ref string stringParam, string parameterName)
            {
                Setter setStr = new Setter(SetValue);
                if (parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault() != null)
                {
                    var data = parameters.Where(s => ((Tuple<string, object>)s).Item1 == parameterName).FirstOrDefault();
                    setStr(ref stringParam, ((Tuple<string, object>)data).Item2);
                }
            }
            private void SetBValue(ref bool lv, object obj)
            {
                try
                {
                    lv = Convert.ToBoolean(obj);
                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex, true);
                }
            }
            private void SetDValue(ref decimal lv, object obj)
            {
                try
                {
                    lv = Convert.ToDecimal(obj);
                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex, true);
                }
            }
            private void SetFValue(ref float lv, object obj)
            {
                try
                {
                    lv = (float)Convert.ToDecimal(obj);
                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex, true);
                }
            }
            private void SetGlobalSetValVariables(object[] parameters) // 전역 변수 사용 구현 항목
            {
                PropertyMappingFnBool(parameters, ref CreateItem.booleanParam, GetVariableName(() => CreateItem.booleanParam));
                PropertyMappingFnStr(parameters, ref CreateItem.strParam, GetVariableName(() => CreateItem.strParam));
                PropertyMappingFnInt(parameters, ref CreateItem.intParam, GetVariableName(() => CreateItem.intParam));
                PropertyMappingFnFloat(parameters, ref CreateItem.floatParam, GetVariableName(() => CreateItem.floatParam));
                PropertyMappingFnDecimal(parameters, ref CreateItem.decimalParam, GetVariableName(() => CreateItem.decimalParam));
                PropertyMappingFnObj(parameters, ref CreateItem.objParam, GetVariableName(() => CreateItem.objParam));

                List<Tuple<string, object>> ListObjectArray = new List<Tuple<string, object>>
                {
                    new Tuple<string, object>("PageItemList", CreateItem.LstInt),
                    new Tuple<string, object>("groupColumn", CreateItem.LstObj),
                    new Tuple<string, object>("ubfunction", CreateItem.LstStr),
                    new Tuple<string, object>("visibleParam", CreateItem.LstMapObj),
                    new Tuple<string, object>("propertyListAr", CreateItem.LstMapStr),
                    new Tuple<string, object>("RequiredValueList", CreateItem.copyObj),
                    new Tuple<string, object>("tabIndexList", CreateItem.scriptParser)
                };
                ListObjectArray.ForEach((e) => { CreateItem.ListToObjectCopy(e.Item2, parameters, e.Item1); });
            }
            private void SetIValue(ref int lv, object obj)
            {
                try
                {
                    lv = Convert.ToInt32(obj);
                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex, true);
                }
            }
            private List<Dictionary<string, object>> ItemsClone(Obj obj) // 지역 변수 사용 구현 항목
            {
                ParameterCollection paramCol = new ParameterCollection();
                ItemExportManager itemExportManager = new ItemExportManager();
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                // 이 지역 안에서만 처리 가능
                Obj copy = CreateItem.CopyObject(obj, null); // 객체 딥카피
                Dictionary<string, object> TableItem = copy.pageMap;
                try
                {

                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex, true);
                }
                return result;
            }
            public object TableProperyKeyCheck(object target, string key)
            {
                object rtn = null;
                if (string.IsNullOrEmpty(key))
                {
                    return null;
                }
                if (target == null)
                {
                    return null;
                }
                else
                {
                    if (target.GetType().Equals(typeof(Dictionary<string, object>)))
                    {
                        if (((Dictionary<string, object>)target).ContainsKey(key))
                        {
                            rtn = ((Dictionary<string, object>)target)[key];
                        }
                        else
                        {
                            rtn = null;
                        }
                    }
                }
                return rtn;
            }

            private void SetValue(ref string lv, object obj)
            {
                try
                {
                    lv = Convert.ToString(obj);
                }
                catch (Exception ex)
                {
                    Program.WriteLog(ex, true);
                }
            }
            private void SuccessFailCallback(Func<bool> work, Action success, Action failure)
            {
                if (work()) success();
                else failure();
            }
            #endregion
        }
    }
    public static class CreateItem
    {
        #region 전역 변수
        public static List<int> LstInt = new List<int>();
        public static List<float> LstFloat = new List<float>();
        public static List<object> LstObj = new List<object>();
        public static List<string> LstStr = new List<string>();
        public static List<Dictionary<string, object>> LstMapObj = new List<Dictionary<string, object>>();
        public static List<Dictionary<string, string>> LstMapStr = new List<Dictionary<string, string>>();
        public static List<List<string>> LstInLstStr = new List<List<string>>();
        public static Dictionary<string, int> mapInt = new Dictionary<string, int>();
        public static Dictionary<string, string> mapStr = new Dictionary<string, string>();
        public static Dictionary<string, object> mapObj = new Dictionary<string, object>();
        public static Dictionary<string, Dictionary<string, object>> mapInMapObj = new Dictionary<string, Dictionary<string, object>>();
        public static Dictionary<string, List<Dictionary<string, object>>> mapInListMapObj = new Dictionary<string, List<Dictionary<string, object>>>();
        public static Obj copyObj;
        public static JavaScriptParser scriptParser;

        public static bool booleanParam = false;
        public static string strParam = "";
        public static int intParam = 0;
        public static float floatParam = 0f;
        public static decimal decimalParam = 0;
        public static object objParam = null;
        #endregion

        private static T Clone<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
        public static void DeepCopyObject(object[] parameters) // 깊은 복사 구현 항목 (변수가 들어있는 객체)
        {
            Obj origin = new Obj(parameters);
            copyObj = origin;
            object[] setParam = (object[])parameters.Clone();
            //ChangeValue(ref setParam, "bb", 123); // 임의의 키값을 변경하는 로직
            Obj copy = CopyObject(origin, setParam); // 객체 딥카피     
        }
        public static Obj CopyObject(Obj origin, object[] parameters)
        {
            Obj CopyObj = origin.Clone();
            copyObj = CopyObj;
            return CopyObj;
        }

        public static void ListToObjectCopy<T>(this T obj, object[] parameters, string objName)
        {
            object result = null;
            if (obj != null)
            {
                copyObj = CopyObject(copyObj,null);
                if (obj.GetType().Equals(typeof(List<Dictionary<string, object>>)))
                {
                    result = new List<Dictionary<string, object>>();
                    copyObj.PropertyMappingFnObj(parameters, ref result, objName);
                }
                else if (obj.GetType().Equals(typeof(List<float>)))
                {
                    result = new List<float>();
                    copyObj.PropertyMappingFnObj(parameters, ref result, objName);
                }
                else if (obj.GetType().Equals(typeof(List<int>)))
                {
                    result = new List<int>();
                    copyObj.PropertyMappingFnObj(parameters, ref result, objName);
                }
                else if (obj.GetType().Equals(typeof(List<object>)))
                {
                    result = new List<object>();
                    copyObj.PropertyMappingFnObj(parameters, ref result, objName);
                }
                else if (obj.GetType().Equals(typeof(List<string>)))
                {
                    result = new List<string>();
                    copyObj.PropertyMappingFnObj(parameters, ref result, objName);
                }
            }
        }
    }
}
