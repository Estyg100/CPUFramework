using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CPUFramework
{
    public class bizObject : INotifyPropertyChanged
    {

        string _typename = "";
        string _tablename = "";
        string _getsproc = "";
        string _updatesproc = "";
        string _deletesproc = "";
        string _primarykeyname = "";
        string _primarykeyparamname = "";
        DataTable _datatable = new();
        List<PropertyInfo> _properties = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bizObject()
        {
            Type t = this.GetType();
            _typename = t.Name;
            _tablename = _typename;
            if (_tablename.ToLower().StartsWith("biz"))
            {
                _tablename = _tablename.Substring(3);
            }
            _getsproc = _tablename + "Get";
            _updatesproc = _tablename + "Update";
            _deletesproc = _tablename + "Delete";
            _primarykeyname = _tablename + "Id";
            _primarykeyparamname = "@" + _primarykeyname;
            _properties = t.GetProperties().ToList<PropertyInfo>();
        }

        public DataTable Load(int primarykeyvalue)
        {
            DataTable dt = new();
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getsproc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, primarykeyvalue);
            dt = SQLUtility.GetDataTable(cmd);
            if (dt.Rows.Count > 0)
            {
                LoadProps(dt.Rows[0]);
            }
            _datatable = dt;
            return dt;
        }

        private void LoadProps(DataRow dr)
        {
            foreach (DataColumn col in dr.Table.Columns)
            {
                SetProp(col.ColumnName, dr[col.ColumnName]);
            }
        }

        public void Delete(int id)
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(_deletesproc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, id);
            SQLUtility.ExecuteSQL(cmd);
        }

        public void Delete()
        {
            PropertyInfo? prop = GetProp(_primarykeyparamname, true, false);
            if (prop != null)
            {
                object? id = prop.GetValue(this);
                if (id != null)
                {
                    this.Delete((int)id);
                }
            }
        }

        public void Save()
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(_updatesproc);
            foreach (SqlParameter param in cmd.Parameters)
            {
                var prop = GetProp(param.ParameterName, true, false);
                if (prop != null && param.Direction != ParameterDirection.Output && param.Direction != ParameterDirection.InputOutput)
                {
                    object? val = prop.GetValue(this);
                    param.Value = val ?? DBNull.Value;
                }
                else if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                {
                    param.Value = DBNull.Value;
                }
            }
            SQLUtility.ExecuteSQL(cmd);
            foreach (SqlParameter param in cmd.Parameters)
            {
                if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                {
                    var prop = GetProp(param.ParameterName, false, true);
                    if (prop != null)
                    {
                        SetProp(param.ParameterName, param.Value == DBNull.Value ? null : param.Value);
                    }
                }
            }
        }

        public void Delete(DataTable dt)
        {
            int primarykeyvalue = (int)dt.Rows[0][_primarykeyname];
            this.Delete(primarykeyvalue);
        }

        public void Save(DataTable dt)
        {
            if (dt.Rows.Count == 0)
            {
                throw new Exception("Cannot call save method because there are no rows in the table.");
            }
            DataRow r = dt.Rows[0];
            SQLUtility.SaveDataRow(r, _updatesproc);
            Debug.Print("-------------------------------");
        }

        private PropertyInfo? GetProp(string propname, bool forrread, bool forwrite)
        {
            propname = propname.ToLower();
            if (propname.StartsWith("@"))
            {
                propname = propname.Substring(1);
            }
            PropertyInfo? prop = _properties.FirstOrDefault(p =>
            p.Name.ToLower() == propname
            && (forrread == false || p.CanRead == true)
            && (forwrite == false || p.CanWrite == true)
            );
            return prop;
        }

        private void SetProp(string propname, object? value)
        {
            var prop = GetProp(propname, false, true);
            if (prop != null)
            {
                if (value == DBNull.Value)
                {
                    value = null;
                }
                try
                {
                    prop.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    string msg = $"{_typename}.{prop.Name} is being set to {value.ToString()} which is the wrong data type. {ex.Message}";
                    throw new CPUDevException(msg, ex);
                }
            }
        }

        protected void InvokePropertyChanged([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
