using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace BotApp
{
    //[Serializable]
    public class CommonFunction
    {
        enum ContactPersonenum
        {
            sch_UK_Admin = 6,
            sch_UK_senior_Admin,
            sch_Acturies,
            sch_Consultant,
            sch_Owner,
            sch_Ops_Owner,
            sch_Client_Manager,
            sch_Pensioner_Payroll,
            sch_Team_Manager,
            sch_PFA,
            sch_Admin_Office
        };
        public static string GetSingleSQLRecord(string p_Query)
        {
            string reader;
            string SQLQuery =p_Query;

            string SQLConnectionString = ConfigurationManager.ConnectionStrings["ChatBot"].ToString();

            using (SqlConnection connection = new SqlConnection(SQLConnectionString))
            {
                SqlCommand command = new SqlCommand(SQLQuery, connection);
                
                connection.Open();
                reader = command.ExecuteScalar().ToString();
                connection.Close();                
     
            }

            return reader;
        }

        public static DataTable GetDTForQuery(string p_Query)
        {
            DataTable reader = new DataTable();
            string SQLQuery = p_Query;

            string SQLConnectionString = ConfigurationManager.ConnectionStrings["ChatBot"].ToString();

            using (SqlConnection connection = new SqlConnection(SQLConnectionString))
            {
                SqlCommand command = new SqlCommand(SQLQuery, connection);

                connection.Open();
                reader.Load(command.ExecuteReader());
                connection.Close();

            }

            return reader;
        }

        public static string returnContactDetails(DataTable _dt, string _contactPerson)
        {
            if(_contactPerson == "uk admin manager" || _contactPerson == "uk administration manager" || _contactPerson == "uk admin" || _contactPerson == "uk administration")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_UK_Admin].ToString();
            }
            if(_contactPerson == "scheme actuary" || _contactPerson == "scheme actuaries" || _contactPerson == "actuaries" || _contactPerson == "actuary")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Acturies].ToString();
            }
            if (_contactPerson == "scheme consultant" || _contactPerson == "consultant")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Consultant].ToString();
            }
            if (_contactPerson == "scheme manager" || _contactPerson == "manager" || _contactPerson == "team manager")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Team_Manager].ToString();
            }
            if (_contactPerson == "scheme owner" || _contactPerson == "owner")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Owner].ToString();
            }
            if (_contactPerson == "senior admin" || _contactPerson == "senior admin manager" || _contactPerson == "uk senior admin" || _contactPerson == "uk senior admin manager" || _contactPerson == "uk senior administrator manager")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_UK_senior_Admin].ToString();
            }
            if (_contactPerson == "ops owner")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Ops_Owner].ToString();
            }
            if (_contactPerson == "client manager")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Client_Manager].ToString();
            }
            if (_contactPerson == "pensioner payroll")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Pensioner_Payroll].ToString();
            }
            if (_contactPerson == "pfa")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_PFA].ToString();
            }
            if (_contactPerson == "admin office" || _contactPerson == "administrator office")
            {
                return _dt.Rows[0][(int)ContactPersonenum.sch_Admin_Office].ToString();
            }
            return null;
        }

        public static float interpolated_TVCFactor(int years, int months, float TVCFac1, float TVCFact2)
        {
            float TVCPerMonth;
            float ipTVCFactor;

            TVCPerMonth = (TVCFact2 - TVCFac1) / 12;
            ipTVCFactor = TVCFac1 + (months * TVCPerMonth);
            return ipTVCFactor;
        }
    }
}