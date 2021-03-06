using Microsoft.Office.InfoPath;
using System;
using System.Xml;
using System.Xml.XPath;
using System.Web;
using System.Collections.Generic;
using Microsoft.SharePoint;
using System.Text;
using System.Net;
using DahiliOtoSatis.TofasUserProfileService;
using System.Data.SqlClient;
using System.Data;

namespace DahiliOtoSatis
{
    public partial class FormCode
    {
        //private Dictionary<String, String> parameters;
        public Dictionary<String, String> Parameters
        {
            get
            {
                if (FormState["parameters"] == null)
                    FormState["parameters"] = GetParameters("DOS.Config", "http://tofaswebportal/otomotiv");
                return (Dictionary<String, String>)FormState["parameters"];
                //    if (parameters == null)
                //    parameters = GetParameters("DOS.Config");
                //return parameters;
            }
        }
        public void InternalStartup()
        {
            EventManager.FormEvents.Loading += new LoadingEventHandler(FormEvents_Loading);
            ((ButtonEvent)EventManager.ControlEvents["btnSubmit"]).Clicked += new ClickedEventHandler(btnSubmit_Clicked);
        }

        public void FormEvents_Loading(object sender, LoadingEventArgs e)
        {
            e.SetDefaultView("New");
            string userName = Application.User.LoginName.Substring(Application.User.LoginName.Length - 6, 6);
            
            if (String.IsNullOrEmpty(GetField("FormName").Value))
            {
                SPListItem existForm = GetDataFromListByUserID(userName+".xml", Parameters["DOS.ListName.DahiliOtoSatisBasvurular"], Parameters["DOS.SiteUrl"], Parameters["DOS.FieldName.FormName"]);
                if (existForm != null)
                {
                    //Kayıtlı form var ise, 
                    GetField("IsValid").SetValue("false");
                    GetField("ValidationString").SetValue(Parameters["ValidationText.ExistForm"]);
                    e.SetDefaultView("MessageView");
                }
            }
                GetSqlUserData(userName);
                //GetUserInformations(Application.User.LoginName);

                if (String.IsNullOrEmpty(GetField("Sicil").Value))
                {
                    GetField("IsValid").SetValue("false");
                    GetField("ValidationString").SetValue(Parameters["ValidationText.ID"]);
                    e.SetDefaultView("MessageView");
                }
                if (String.IsNullOrEmpty(GetField("IseGirisTarihi").Value) || String.IsNullOrEmpty(GetField("Unvan").Value))
                {
                    GetField("IsValid").SetValue("false");
                    GetField("ValidationString").SetValue(Parameters["ValidationText.NoTitleAndEmploymentDate"]);
                    e.SetDefaultView("MessageView");
                }
                else
                {
                    SetFields(GetField("Sicil").Value);
                }
        }

        public void IsValid()
        {
            bool isValid = true;
            StringBuilder validationString = new StringBuilder();

            TimeSpan iseGirisFark = DateTime.Today - DateTime.Parse(GetField("IseGirisTarihi").Value);
            if (iseGirisFark.TotalDays <= 180 && GetField("Unvan").Value == Parameters["DOS.ValidationTitle"])
            {
                isValid = false;
                validationString.AppendLine(Parameters["ValidationText.Title"]);
            }
            if (!String.IsNullOrEmpty(GetField("BlockedFinishDate").Value))
            {
                if (GetField("IsBlocked").Value.ToLower() == "true" && DateTime.Parse(GetField("BlockedFinishDate").Value) > DateTime.Today)
                {
                    isValid = false;
                    validationString.AppendLine(Parameters["ValidationText.IsBlocked"]);
                }
            }
            if (!String.IsNullOrEmpty(GetField("LastVehiclePurchaseDate").Value))
            {
                DateTime dateLastVehiclePurchaseDate = DateTime.Parse(GetField("LastVehiclePurchaseDate").Value);
                TimeSpan sonAracAlimFark = DateTime.Today - dateLastVehiclePurchaseDate;
                if (sonAracAlimFark.TotalDays <= 730)
                {
                    isValid = false;
                    validationString.AppendLine(Parameters["ValidationText.LastVehiclePurchaseDate"]);
                }
            }
            if (GetField("ChangingCount").Value == "3")
            {
                isValid = false;
                validationString.AppendLine(Parameters["ValidationText.ChangingCount"]);
            }

            GetField("IsValid").SetValue(isValid.ToString());
            GetField("ValidationString").SetValue(validationString.ToString());
        }

        private XPathNavigator GetField(string fieldName)
        {
            XPathNavigator navigator = MainDataSource.CreateNavigator().SelectSingleNode(String.Format("/my:myFields/my:{0}", fieldName), NamespaceManager);
            return navigator;
        }

        private void GetSqlUserData(string TUserName)
        {
            string command = String.Format("SELECT * FROM [TofasUserProfile].[dbo].[HCM_SPWEB] where TUSERNAME = '{0}'", TUserName);
            string conStr = "Data Source=TF2SPD01;Initial Catalog=TofasUserProfile;User=tofasuserprofile;Password=Smart123;";
            using (SqlConnection sqlConn = new SqlConnection(conStr))
	        {
                SqlDataAdapter adapter = new SqlDataAdapter(command, sqlConn);

                DataTable tblUsers = new DataTable();
                adapter.Fill(tblUsers);

                if(tblUsers.Rows.Count >0)
                {
                    GetField("Sicil").SetValue(tblUsers.Rows[0]["TUSERNAME"].ToString());
                    GetField("AdSoyad").SetValue(tblUsers.Rows[0]["FULLNAME"].ToString());
                    GetField("AccountName").SetValue(tblUsers.Rows[0]["USERNAME"].ToString());
                    GetField("EPosta").SetValue(tblUsers.Rows[0]["EMAIL"].ToString());
                    GetField("GSMTelNo").SetValue(tblUsers.Rows[0]["TELEFON"].ToString());
                    GetField("DahiliNo").SetValue(tblUsers.Rows[0]["TELEFON"].ToString());
                    GetField("EvTelNo").SetValue(tblUsers.Rows[0]["TELEFON"].ToString());
                    GetField("Unvan").SetValue(tblUsers.Rows[0]["CALISAN_ALT_GRUBU_KODU"].ToString());
                    
                    DateTime employmentDate = DateTime.Parse(tblUsers.Rows[0]["ISE_GIRIS_TARIHI"].ToString());
                    GetField("IseGirisTarihi").SetValue(String.Format("{1}/{0}/{2}", employmentDate.Month, employmentDate.Day, employmentDate.Year));
                }
	        }
        }

        private void GetUserInformations(string username)
        {
            XPathNavigator myRoot = MainDataSource.CreateNavigator();
            TofasUserProfileService.UserProfileService profileService = new TofasUserProfileService.UserProfileService();
            profileService.UseDefaultCredentials = true; 
            //profileService.Credentials = new NetworkCredential("spsadmin", "SpS@18500", "tofas"); 
            TofasUserProfileService.PropertyData[] userProps = null;
            try
            {
                userProps = profileService.GetUserProfileByName(username);
            }
            catch { }
            if (userProps == null || userProps.Length == 0)
            {
                return;
            }

            for (int i = 0; i < userProps.Length; i++)
            {
                XPathNavigator node = null;
                switch (userProps[i].Name.ToLower())
                {
                    case "sicil":
                        node = myRoot.SelectSingleNode("/my:myFields/my:Sicil", NamespaceManager);
                        break;
                    case "preferredname":
                        node = myRoot.SelectSingleNode("/my:myFields/my:AdSoyad", NamespaceManager);
                        break;
                    case "accountname":
                        node = myRoot.SelectSingleNode("/my:myFields/my:AccountName", NamespaceManager);
                        break;
                    case "workemail":
                        node = myRoot.SelectSingleNode("/my:myFields/my:EPosta", NamespaceManager);
                        break;
                    case "workphone":
                        node = myRoot.SelectSingleNode("/my:myFields/my:GSMTelNo", NamespaceManager);
                        break;
                    case "interphone":
                        node = myRoot.SelectSingleNode("/my:myFields/my:DahiliNo", NamespaceManager);
                        break;
                    case "homephone":
                        node = myRoot.SelectSingleNode("/my:myFields/my:EvTelNo", NamespaceManager);
                        break;
                    case "title":
                        node = myRoot.SelectSingleNode("/my:myFields/my:Unvan", NamespaceManager);
                        break;
                    case "employmentdate":
                        node = myRoot.SelectSingleNode("/my:myFields/my:IseGirisTarihi", NamespaceManager);
                        break;
                    default:
                        continue;
                }
                TofasUserProfileService.ValueData[] values = userProps[i].Values;
                if (values.Length > 0)
                {
                    if (node != null && !string.IsNullOrEmpty(values[0].Value.ToString()))
                    {
                        if (userProps[i].Name.ToLower() == "employmentdate")
                        {
                            DeleteNil(GetField("IseGirisTarihi"));
                            DateTime dateEmploymentdate = (DateTime)values[0].Value;
                            node.SetValue(String.Format("{1}/{0}/{2}", dateEmploymentdate.Month, dateEmploymentdate.Day, dateEmploymentdate.Year));
                        }
                        else
                            node.SetValue(values[0].Value.ToString());
                    }
                }
            }
        }

        private SPListItem GetDataFromListByUserID(string userID, string listName, string siteUrl, string fieldName)
        {
            SPQuery query = new SPQuery();
            query.Query = String.Format(@"  <Where>
                                                <Eq>
                                                    <FieldRef Name='{1}' />
                                                    <Value Type='Text'>{0}</Value>
                                                </Eq>
                                            </Where>", userID, fieldName);
            SPListItem item = null;

            SPSecurity.RunWithElevatedPrivileges(
            delegate()
            {
                using (SPSite site = new SPSite(siteUrl))
                {
                    using (SPWeb web = site.OpenWeb())
                    {
                        SPListItemCollection items = web.Lists[listName].GetItems(query);

                        item = (items != null && items.Count > 0) ? items[0] : null;
                    }
                }
            });
            return item;
        }

        private void SetFields(string userID)
        {
            SPListItem itemAracAlimIstisnalari = GetDataFromListByUserID(userID, Parameters["DOS.ListName.AracAlimIstisnalari"], Parameters["DOS.SiteUrl"], "Sicil");
            //SPListItem itemBasvurular = GetDataFromListByUserID(userID, Parameters["DOS.ListName.Basvurular"], subSiteUrl);

            if (itemAracAlimIstisnalari != null)
            {
                DateTime? dateLastVehiclePurchaseDate = itemAracAlimIstisnalari["SonAracAlimTarihi"] != null ? (DateTime?)itemAracAlimIstisnalari["SonAracAlimTarihi"] : (DateTime?)null;
                DateTime? dateBlockedFinishDate = itemAracAlimIstisnalari["CezaBitisTarihi"] != null ? (DateTime?)itemAracAlimIstisnalari["CezaBitisTarihi"] : (DateTime?)null;

                if (dateLastVehiclePurchaseDate != null)
                {
                    DeleteNil(GetField("LastVehiclePurchaseDate"));
                    GetField("LastVehiclePurchaseDate").SetValue(String.Format("{1}/{0}/{2}", dateLastVehiclePurchaseDate.Value.Month, dateLastVehiclePurchaseDate.Value.Day, dateLastVehiclePurchaseDate.Value.Year));
                }

                if (dateBlockedFinishDate != null)
                {
                    DeleteNil(GetField("BlockedFinishDate"));
                    GetField("BlockedFinishDate").SetValue(String.Format("{1}/{0}/{2}", dateBlockedFinishDate.Value.Month, dateBlockedFinishDate.Value.Day, dateBlockedFinishDate.Value.Year));
                }

                GetField("IsBlocked").SetValue(itemAracAlimIstisnalari["Cezalimi"].ToString());
            }
        }

        public void DeleteNil(XPathNavigator node)
        {
            if (node.MoveToAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance"))
                node.DeleteSelf();
        }

        public static Dictionary<string, string> GetParameters(String listName, string siteUrl)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            SPSecurity.RunWithElevatedPrivileges(
                    delegate()
                    {
                        using (SPSite site = new SPSite(siteUrl))
                        {
                            using (SPWeb web = site.OpenWeb())
                            {
                                SPList listInitiator = web.Lists[listName];
                                SPListItemCollection collItem = listInitiator.Items;
                                foreach (SPListItem listItem in collItem)
                                {
                                    parameters.Add(listItem["Title"].ToString(), listItem["Value"].ToString());
                                }
                            }
                        }
                    }
                );
            return parameters;
        }

        public void btnSubmit_Clicked(object sender, ClickedEventArgs e)
        {
            IsValid();
            if (GetField("IsValid").Value.ToLower() == "true")
            {
                GetField("FormName").SetValue(GetField("Sicil").Value);
                int changingCount = GetField("ChangingCount").ValueAsInt + 1;
                GetField("ChangingCount").SetValue(changingCount.ToString());
                this.Submit();
            }
            else
            {
                this.ViewInfos.SwitchView("MessageView");
            }
        }
    }
}
