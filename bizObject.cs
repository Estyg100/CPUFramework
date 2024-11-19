using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace CPUFramework
{
    public class bizObject
    {

        string _tablename = "";
        string _getsproc = "";
        string _updatesproc = "";
        string _deletesproc = "";
        string _primarykeyname = "";
        string _primarykeyparamname = "";
        DataTable _datatable = new();

        public bizObject(string tablename)
        {
            _tablename = tablename;
            _getsproc = tablename + "Get";
            _updatesproc = tablename + "Update";
            _deletesproc = tablename + "Delete";
            _primarykeyname = tablename + "Id";
            _primarykeyparamname = "@" + _primarykeyname;
        }

        public DataTable Load(int primarykeyvalue)
        {
            DataTable dt = new();
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getsproc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, primarykeyvalue);
            dt = SQLUtility.GetDataTable(cmd);
            _datatable = dt;
            return dt;
        }

        public void Delete(DataTable dt)
        {
            int primarykeyvalue = (int)dt.Rows[0][_primarykeyname];
            SqlCommand cmd = SQLUtility.GetSqlCommand(_deletesproc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, primarykeyvalue);
            SQLUtility.ExecuteSQL(cmd);
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
    }
}
