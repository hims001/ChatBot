using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BotApp.Dialogs
{
    [LuisModel("452206ce-611b-4ae5-8e6d-bdd9d9434d3c", "1ae61d322f7b40d88e5f8793b4af5383", LuisApiVersion.V2, SpellCheck = true, Log = true)]
    [Serializable]
    class profomaDialog : LuisDialog<profomaOption>
    {

        string schemeSelected;
        bool userIntentset;
        string SchemeOldCategory;
        string SchemeCategory;
        string SchemeSubCategory;
        string strMsg;
        string strAge;
        int ContactPersonCount;
        int ageyears;
        int ageMonths;
        List<string> ContactPerson;
        string strThank = "Thank you for using the service. \n\nHave a nice day!";

        enum IntentSelected
        {
            None,
            MinimumAge,
            maximumAge,
            AccrualRates,
            LowerAccrualRates,
            MiddleAccrualRates,
            UpperAccrualRates,
            ContactDetails,
            TVCFactor,
            Hello,
            EndConversation
        };
        enum PromptDialogFor
        {
            None,
            SchemeName,
            CategoryName,
            SubCategoryName,
            OldCategory,
            ConfirmScheme,
            ContactPerson,
            TVCAge,
        };

        PromptDialogFor PromptDialogCount;
        List<String> AccrualType = new List<string>();

        IntentSelected userIntent = IntentSelected.None;
        public async Task StartAsync(IDialogContext context)
        {

            context.Wait(MessageReceived);

            /*return Task.CompletedTask;*/
        }

        [LuisIntent("Hello")]
        public async Task Greet(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi, How can I help you?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("MinimumAge")]

        public async Task GetSchemeMinimumAge(IDialogContext context, LuisResult result)
        {
            var entities = new List<EntityRecommendation>(result.Entities);
            string UserQuery = result.Query;

            if (entities.Count != 0)
            {
                foreach (var entity in result.Entities)
                {
                    if (entity.Type == "scheme")
                    {
                        schemeSelected = UserQuery.Substring((int)entity.StartIndex, (int)(entity.EndIndex - entity.StartIndex + 1));
                        if (schemeSelected.ToLower() != "scheme")
                        {
                            await context.PostAsync("minimum age of Query entity is " + schemeSelected);
                            userIntent = IntentSelected.MinimumAge;
                        }
                        else
                        {
                            await context.PostAsync("Please Enter the scheme no.");
                            userIntent = IntentSelected.MinimumAge;
                        }
                    }
                }
            }
            else
            {
                await context.PostAsync("Please Enter the scheme no.");
                userIntent = IntentSelected.MinimumAge;
            }
            context.Wait(MessageReceived);
        }

        [LuisIntent("MaximumAge")]

        public async Task GetSchememaximumAge(IDialogContext context, LuisResult result)
        {
            var entities = new List<EntityRecommendation>(result.Entities);
            string UserQuery = result.Query;

            if (entities.Count != 0)
            {
                foreach (var entity in result.Entities)
                {
                    if (entity.Type == "scheme")
                    {
                        schemeSelected = UserQuery.Substring((int)entity.StartIndex, (int)(entity.EndIndex - entity.StartIndex + 1));
                        if (schemeSelected.ToLower() != "scheme")
                        {
                            await context.PostAsync("maximum age of Query entity is " + schemeSelected);
                            userIntent = IntentSelected.maximumAge;
                        }
                        else
                        {
                            await context.PostAsync("Please Enter the scheme no.");
                            userIntent = IntentSelected.maximumAge;
                        }

                    }
                }
            }
            else
            {
                await context.PostAsync("Please Enter the scheme no.");
                userIntent = IntentSelected.maximumAge;
            }
            context.Wait(MessageReceived);
        }

        [LuisIntent("AccrualRate")]
        public async Task GetAccrualRate(IDialogContext context, LuisResult result)
        {
            schemeSelected = "";
            SchemeCategory = "";
            SchemeSubCategory = "";
            SchemeOldCategory = "";
            string SQLQuery;
            string RepeativeCheck;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            //CommonFunction commonObject = new CommonFunction();

            try
            {
                if (userIntent == IntentSelected.None)
                {
                    var entities = new List<EntityRecommendation>(result.Entities);
                    string UserQuery = result.Query;
                    schemeSelected = "";

                    if (entities.Count == 0)
                    {
                        userIntent = IntentSelected.AccrualRates;
                        PromptDialogCount = PromptDialogFor.SchemeName;
                        PromptDialog.Text(context, ProcessAccrualRate, "Please provide the scheme name");
                    }
                    else
                    {
                        foreach (var entity in entities)
                        {
                            if (entity.Type == "Scheme")
                            {
                                schemeSelected = entity.Entity;
                            }
                            else if (entity.Type == "Category")
                            {
                                SchemeCategory = entity.Entity;
                            }
                        }


                        //await context.PostAsync(schemeSelected + "\n\n" + SchemeCategory);

                        SQLQuery = "select * from Scheme INNER JOIN AccrualRate ON acr_schemeNo = sch_no " +
                                "where LOWER(sch_description) = '" + schemeSelected + "' " +
                                (SchemeCategory == "" ? "" : "and LOWER(acr_catagoryDescription) = '" + SchemeCategory + "'");
                        dt = CommonFunction.GetDTForQuery(SQLQuery);

                        if (dt.Rows.Count == 0)
                        {
                            userIntent = IntentSelected.AccrualRates;
                            PromptDialogCount = PromptDialogFor.SchemeName;
                            PromptDialog.Text(context, ProcessAccrualRate, "Provided scheme name " + (SchemeCategory == "" ? "" : "or catagory name ") +
                                "does not match in my record can you please provide me the scheme name");
                        }
                        else if (dt.Rows.Count == 1)
                        {
                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;
                            await context.PostAsync("The Accrual Rate is '" + dt.Rows[0]["acr_accrualRateValue"].ToString());
                            context.Wait(MessageReceived);
                        }
                        else if (dt.Rows.Count > 1)
                        {
                            if (SchemeCategory == "")
                            {
                                optionList.Clear();
                                RepeativeCheck = "";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    if (RepeativeCheck != dt.Rows[i]["acr_catagoryDescription"] + "_" + dt.Rows[i]["acr_PreviousCategoryDescription"])
                                    {
                                        RepeativeCheck = dt.Rows[i]["acr_catagoryDescription"] + "_" + dt.Rows[i]["acr_PreviousCategoryDescription"];
                                        optionList.Add(dt.Rows[i]["acr_catagoryDescription"].ToString() +
                                            (dt.Rows[i]["acr_PreviousCategoryDescription"].ToString() != "" ?
                                            "Old Category:" + dt.Rows[i]["acr_PreviousCategoryDescription"].ToString() : ""));
                                    }

                                }
                                PromptDialogCount = PromptDialogFor.CategoryName;
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "the provided scheme have more then one category, Please Select the category", "Not the valid option", 3, PromptStyle.Auto);
                            }
                            else
                            {
                                optionList.Clear();
                                RepeativeCheck = "";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    if (RepeativeCheck != dt.Rows[i]["acr_catagoryDescription"] + "_" + dt.Rows[i]["acr_PreviousCategoryDescription"])
                                    {
                                        RepeativeCheck = dt.Rows[i]["acr_catagoryDescription"] + "_" + dt.Rows[i]["acr_PreviousCategoryDescription"];
                                        optionList.Add(dt.Rows[i]["acr_catagoryDescription"].ToString() +
                                            (dt.Rows[i]["acr_PreviousCategoryDescription"].ToString() != "" ?
                                            "Old Category:" + dt.Rows[i]["acr_PreviousCategoryDescription"].ToString() : ""));
                                    }

                                }

                                if (optionList.Count != 0)
                                {
                                    PromptDialogCount = PromptDialogFor.CategoryName;
                                    PromptDialog.Choice(context, ProcessAccrualRate, optionList, "the provided scheme have more then one historic category, Please Select the revelent category", "Not the valid option", 3, PromptStyle.Auto);
                                }
                                else
                                {
                                    optionList.Clear();
                                    RepeativeCheck = "";
                                    for (int i = 0; i < dt.Rows.Count; i++)
                                    {
                                        if (RepeativeCheck != dt.Rows[i]["acr_subCategory"].ToString())
                                        {
                                            RepeativeCheck = dt.Rows[i]["acr_subCategory"].ToString();
                                            optionList.Add(dt.Rows[i]["acr_subCategory"].ToString());
                                        }

                                    }
                                    PromptDialogCount = PromptDialogFor.SubCategoryName;
                                    PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please Select the sub category", "Not the valid option", 3, PromptStyle.Auto);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync(ex.Message);
                context.Wait(MessageReceived);
            }


        }

        public async Task ProcessAccrualRate(IDialogContext context, IAwaitable<string> result)
        {
            //var AccrualOption = null;
            try
            {
                string SQLQuery;
                string SQLConnectionString;
                string RepeativeCheck;
                string AccrualRateNo;
                SqlDataReader reader;
                SqlCommand command;
                DataTable dt = new DataTable();
                List<string> optionList = new List<string>();
                //CommonFunction commonObject = new CommonFunction();

                var UserResponse = await result;

                if ((UserResponse).ToLower() == "cancel" || (UserResponse).ToLower() == "abort" || (UserResponse).ToLower() == "exit")
                {
                    await context.PostAsync(strThank);
                    //PromptDialog.Choice(context, ProcessExit, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    return;
                }

                if (PromptDialogCount == PromptDialogFor.SchemeName || PromptDialogCount == PromptDialogFor.ConfirmScheme)
                {
                    if (PromptDialogCount == PromptDialogFor.SchemeName)
                    {
                        schemeSelected = UserResponse.ToString();
                        if (schemeSelected != "")
                        {
                            SQLQuery = "SELECT * FROM scheme WHERE sch_description LIKE '%" + schemeSelected + "%'";
                            dt = CommonFunction.GetDTForQuery(SQLQuery);

                            if (dt.Rows.Count == 1)
                            {
                                //await context.PostAsync("You you selected the scheme no with ID" + dt.Rows[0]["sch_no"].ToString());
                                optionList.Clear();
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    optionList.Add(dt.Rows[i]["sch_description"].ToString());
                                }
                                PromptDialogCount = PromptDialogFor.ConfirmScheme;
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please confirm the scheme name", "Not the valid option", 3, PromptStyle.Auto);
                            }
                            else if (dt.Rows.Count > 1)
                            {
                                optionList.Clear();
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    optionList.Add(dt.Rows[i]["sch_description"].ToString());
                                }
                                PromptDialogCount = PromptDialogFor.ConfirmScheme;
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "We found more then on scheme with name you mention please select your scheme from bellow given option", "Not the valid option", 3, PromptStyle.Auto);
                            }
                            else if (dt.Rows.Count == 0)
                            {
                                PromptDialog.Text(context, ProcessAccrualRate, "Please provide the correct scheme name");
                            }
                        }
                        else
                        {
                            PromptDialog.Text(context, ProcessAccrualRate, "Please provide the correct scheme name");
                        }
                    }
                    else if (PromptDialogCount == PromptDialogFor.ConfirmScheme)
                    {
                        schemeSelected = UserResponse.ToString();

                        //check for category
                        SQLQuery = "SELECT sch_no FROM Scheme WHERE sch_description = '" + schemeSelected + "'";

                        string schemeID = CommonFunction.GetSingleSQLRecord(SQLQuery);
                        string HasDifferentAccrual = CommonFunction.GetSingleSQLRecord("SELECT sch_hasDifferentAccrualRate FROM Scheme WHERE sch_description = '" + schemeSelected + "'");

                        if (HasDifferentAccrual == "Y")
                        {
                            SQLQuery = "Select * from AccrualRate Where acr_schemeNo = " + schemeID;


                            dt = CommonFunction.GetDTForQuery(SQLQuery);

                            if (dt.Rows.Count == 1)
                            {
                                userIntent = IntentSelected.None;
                                PromptDialogCount = PromptDialogFor.None;
                                await context.PostAsync("The Accrual Rate of '" + schemeSelected + "' is " + dt.Rows[0]["acr_accrualRateValue"].ToString());
                                await context.PostAsync(strThank);
                                //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                                //context.Wait(MessageReceived);
                            }
                            else if (dt.Rows.Count > 1)
                            {
                                optionList.Clear();
                                RepeativeCheck = "";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    if (RepeativeCheck != dt.Rows[i]["acr_catagoryDescription"] + "_" + dt.Rows[i]["acr_PreviousCategoryDescription"])
                                    {
                                        RepeativeCheck = dt.Rows[i]["acr_catagoryDescription"] + "_" + dt.Rows[i]["acr_PreviousCategoryDescription"];
                                        optionList.Add(dt.Rows[i]["acr_catagoryDescription"].ToString() +
                                            (dt.Rows[i]["acr_PreviousCategoryDescription"].ToString() != "" ?
                                            "Old Category:" + dt.Rows[i]["acr_PreviousCategoryDescription"].ToString() : ""));
                                    }

                                }
                                PromptDialogCount = PromptDialogFor.CategoryName;
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please Select the category", "Not the valid option", 3, PromptStyle.Auto);
                            }
                        }
                        else
                        {
                            SQLQuery = "Select Top 1 acr_accrualRateValue from AccrualRate Where acr_schemeNo = " + schemeID;

                            string accrualRate = CommonFunction.GetSingleSQLRecord(SQLQuery);

                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;

                            await context.PostAsync("The Accrual Rate of scheme '" + schemeSelected + " is " + accrualRate);
                            await context.PostAsync(strThank);
                            //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                            //context.Wait(MessageReceived);
                        }
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.CategoryName)
                {
                    SchemeCategory = UserResponse;
                    if (SchemeCategory.Contains("Old Category:"))
                    {
                        SchemeOldCategory = SchemeCategory.Substring(SchemeCategory.IndexOf("Old Category:") + "Old Category:".Length,
                                                SchemeCategory.Length - ((SchemeCategory.IndexOf("Old Category:") + "Old Category:".Length)));
                        SchemeCategory = SchemeCategory.Substring(0, SchemeCategory.IndexOf("Old Category:"));
                    }

                    //await context.PostAsync("Your Category " + SchemeCategory);
                    //await context.PostAsync("Your category " + SchemeOldCategory);

                    SQLQuery = "SELECT * FROM AccrualRate WHERE acr_catagoryDescription = '" + SchemeCategory + "' " +
                        (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : "AND acr_PreviousCategoryDescription = '" + SchemeOldCategory + "'");

                    dt = CommonFunction.GetDTForQuery(SQLQuery);

                    if (dt.Rows.Count == 1)
                    {
                        userIntent = IntentSelected.None;
                        PromptDialogCount = PromptDialogFor.None;
                        await context.PostAsync("The Accrual Rate of scheme '" + schemeSelected + "' with category '" +
                            SchemeCategory + "' " + (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : " And scheme old Category '" + SchemeOldCategory + "'") +
                            "is " + dt.Rows[0]["acr_accrualRateValue"].ToString());
                        await context.PostAsync(strThank);
                        //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                        //context.Wait(MessageReceived);
                    }
                    else if (dt.Rows.Count > 1)
                    {
                        optionList.Clear();
                        RepeativeCheck = "";
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (RepeativeCheck != dt.Rows[i]["acr_subCategory"].ToString())
                            {
                                RepeativeCheck = dt.Rows[i]["acr_subCategory"].ToString();
                                optionList.Add(dt.Rows[i]["acr_subCategory"].ToString());
                            }

                        }
                        PromptDialogCount = PromptDialogFor.SubCategoryName;
                        PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please Select the sub category", "Not the valid option", 3, PromptStyle.Auto);
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.SubCategoryName)
                {
                    SchemeSubCategory = UserResponse.ToString();

                    SQLQuery = "SELECT acr_accrualRateValue FROM AccrualRate WHERE acr_catagoryDescription = '" + SchemeCategory + "' " +
                        (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : "AND acr_PreviousCategoryDescription = '" + SchemeOldCategory + "'") +
                        "AND acr_subCategory = '" + SchemeSubCategory + "'";

                    AccrualRateNo = CommonFunction.GetSingleSQLRecord(SQLQuery);

                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    await context.PostAsync("The Accrual Rate of scheme '" + schemeSelected + "' with category '" +
                            SchemeCategory + "' " + (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : " And scheme old Category '" + SchemeOldCategory + "'") +
                            " and sub Category " + SchemeSubCategory + " is " + AccrualRateNo);
                    await context.PostAsync(strThank);
                    //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                    //context.Wait(MessageReceived);
                }
                else
                {
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    if (UserResponse.ToString().ToLower() == "no")
                    {
                        await context.PostAsync("Sorry to hear that I could not be of more help. In future, feel free to contact.");
                        await context.PostAsync("Have a nice day!");
                    }
                    else
                    {
                        await context.PostAsync("How can I help you?");
                    }
                    context.Wait(MessageReceived);
                }
                //var AccrualOption = await result;
                //if (AccrualOption != null)
                //{
                //    if (AccrualOption == "Middle Rate") { userIntent = IntentSelected.MiddleAccrualRates; }
                //    else if (AccrualOption == "Lower Rate") { userIntent = IntentSelected.LowerAccrualRates; }
                //    else if (AccrualOption == "Upper Rate") { userIntent = IntentSelected.UpperAccrualRates; }


                //}
                //else
                //{
                //    await context.PostAsync("Form returned empty response");
                //}
            }
            catch (OperationCanceledException)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync("You canceled the form");
                context.Wait(MessageReceived);
            }
            catch (TooManyAttemptsException)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync("You have Exausted the Attempt chances");
                context.Wait(MessageReceived);
            }
            catch (Exception ex)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync(ex.Message);
                context.Wait(MessageReceived);
            }


        }

        [LuisIntent("ContactDetails")]
        public async Task GetContactDetails(IDialogContext context, LuisResult result)
        {
            schemeSelected = "";
            ContactPersonCount = 0;
            ContactPerson = new List<string>();

            string SQLQuery;
            string RepeativeCheck;
            string returnedContactPerson;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            //CommonFunction commonObject = new CommonFunction();

            try
            {
                if (userIntent == IntentSelected.None || userIntent == IntentSelected.ContactDetails)
                {
                    var entities = new List<EntityRecommendation>(result.Entities);
                    string UserQuery = result.Query;
                    schemeSelected = "";

                    if (entities.Count == 0)
                    {
                        userIntent = IntentSelected.ContactDetails;
                        PromptDialogCount = PromptDialogFor.ContactPerson;
                        //await context.PostAsync("Sorry! Couldn't understand you \n\n" +
                        //    "Please reframe your query by providing the designation of the person you want to contact");
                        //context.Wait(MessageReceived);
                        PromptDialog.Text(context, ProcessContactDetails, "Please provide the designation of contact person.");
                    }
                    else
                    {
                        foreach (var entity in entities)
                        {
                            if (entity.Type == "Scheme")
                            {
                                schemeSelected = entity.Entity;
                            }
                            else if (entity.Type == "ContactPerson")
                            {
                                ContactPersonCount += 1;
                                ContactPerson.Add(entity.Entity);
                            }
                        }

                        if (schemeSelected != "")
                        {
                            SQLQuery = "SELECT sch_no, sch_description, ContactDetails.* FROM scheme INNER JOIN ContactDetails " +
                                        "ON Scheme.sch_no = ContactDetails.CD_scheme_no " +
                                        "WHERE LOWER(sch_description) = '" + schemeSelected + "'";

                            dt = CommonFunction.GetDTForQuery(SQLQuery);

                            if (dt.Rows.Count == 0)
                            {
                                userIntent = IntentSelected.ContactDetails;
                                PromptDialogCount = PromptDialogFor.SchemeName;
                                PromptDialog.Text(context, ProcessContactDetails, "Provide correct scheme name.");
                            }
                            else if (dt.Rows.Count == 1)
                            {
                                if (ContactPersonCount != 0)
                                {
                                    for (int i = 0; i < ContactPerson.Count; i++)
                                    {
                                        returnedContactPerson = CommonFunction.returnContactDetails(dt, ContactPerson[i]);
                                        if (returnedContactPerson != null && returnedContactPerson != "")
                                        {
                                            await context.PostAsync("Contact details of " + ContactPerson[i] + " --> " + returnedContactPerson);
                                        }
                                        else if (returnedContactPerson == null)
                                        {
                                            await context.PostAsync(ContactPerson[i] + " contact person doesn't exist in our records.");
                                        }
                                        else if (returnedContactPerson == "")
                                        {
                                            await context.PostAsync("Sorry we don't have record for the contact person.");
                                        }
                                    }
                                    await context.PostAsync(strThank);
                                    //PromptDialog.Choice(context, ProcessContactDetails, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                                    userIntent = IntentSelected.None;
                                    PromptDialogCount = PromptDialogFor.None;
                                    //context.Wait(MessageReceived);
                                }
                                else
                                {
                                    strMsg = "1. " + ((dt.Rows[0]["CD_UK_admin"].ToString() == "") ? "" : "UK administrator -> " + dt.Rows[0]["CD_UK_admin"].ToString() + "\n\n") +
                                             "2. " + ((dt.Rows[0]["CD_UK_senior_Admin"].ToString() == "") ? "" : "UK Scheme Senior Administrator manager -> " + dt.Rows[0]["CD_UK_senior_Admin"].ToString() + "\n\n") +
                                             "3. " + ((dt.Rows[0]["CD_Acturies"].ToString() == "") ? "" : "Scheme Actuary -> " + dt.Rows[0]["CD_Acturies"].ToString() + "\n\n") +
                                             "4. " + ((dt.Rows[0]["CD_Consultant"].ToString() == "") ? "" : "Scheme Consultant -> " + dt.Rows[0]["CD_Consultant"].ToString() + "\n\n") +
                                             "5. " + ((dt.Rows[0]["CD_Owner"].ToString() == "") ? "" : "Scheme owner -> " + dt.Rows[0]["CD_Owner"].ToString() + "\n\n") +
                                             "6. " + ((dt.Rows[0]["CD_Ops_Owner"].ToString() == "") ? "" : "ops owner  -> " + dt.Rows[0]["CD_Ops_Owner"].ToString() + "\n\n") +
                                             "7. " + ((dt.Rows[0]["CD_Client_Manager"].ToString() == "") ? "" : "Client manager -> " + dt.Rows[0]["CD_Client_Manager"].ToString() + "\n\n") +
                                             "8. " + ((dt.Rows[0]["CD_Pensioner_Payroll"].ToString() == "") ? "" : "Pensioner payroll -> " + dt.Rows[0]["CD_Pensioner_Payroll"].ToString() + "\n\n") +
                                             "9. " + ((dt.Rows[0]["CD_Team_Manager"].ToString() == "") ? "" : "Scheme Manager -> " + dt.Rows[0]["CD_Team_Manager"].ToString() + "\n\n") +
                                             "10. " + ((dt.Rows[0]["CD_PFA"].ToString() == "") ? "" : "PFA -> " + dt.Rows[0]["CD_PFA"].ToString() + "\n\n") +
                                             "11. " + ((dt.Rows[0]["CD_Admin_Office"].ToString() == "") ? "" : "UK administrator office -> " + dt.Rows[0]["CD_Admin_Office"].ToString());

                                    await context.PostAsync(strMsg);
                                    await context.PostAsync(strThank);
                                    //PromptDialog.Choice(context, ProcessContactDetails, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                                    userIntent = IntentSelected.None;
                                    PromptDialogCount = PromptDialogFor.None;
                                    //context.Wait(MessageReceived);
                                }
                                userIntent = IntentSelected.None;
                                PromptDialogCount = PromptDialogFor.None;
                                context.Wait(MessageReceived);
                            }
                            else
                            {
                                optionList.Clear();
                                RepeativeCheck = "";
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    if (RepeativeCheck != dt.Rows[i]["CD_category_name"] + "_" + dt.Rows[i]["CD_sub_category_name"])
                                    {
                                        RepeativeCheck = dt.Rows[i]["CD_category_name"] + "_" + dt.Rows[i]["CD_sub_category_name"];
                                        optionList.Add(dt.Rows[i]["CD_category_name"].ToString() +
                                            (dt.Rows[i]["CD_sub_category_name"].ToString() != "" ?
                                            "Sub Category:" + dt.Rows[i]["CD_sub_category_name"].ToString() : ""));
                                    }

                                }
                                PromptDialogCount = PromptDialogFor.CategoryName;
                                PromptDialog.Choice(context, ProcessContactDetails, optionList, "the provided scheme have more then one category, Please Select the category", "Not the valid option", 3, PromptStyle.Auto);
                            }
                        }
                        else if (ContactPersonCount != 0 && schemeSelected == "")
                        {
                            userIntent = IntentSelected.ContactDetails;
                            PromptDialogCount = PromptDialogFor.SchemeName;
                            PromptDialog.Text(context, ProcessContactDetails, "Provide scheme name ");
                        }
                        else if (ContactPersonCount == 0 && schemeSelected == "")
                        {
                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;
                            await context.PostAsync("Please reframe your query by providing the designation of the person you want to contact");
                            context.Wait(MessageReceived);
                        }
                    }
                }
                else
                {
                    await context.PostAsync("Please cancel the previous operation");
                    context.Wait(MessageReceived);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task ProcessContactDetails(IDialogContext context, IAwaitable<string> result)
        {
            string SQLQuery;
            string RepeativeCheck;
            string returnedContactPerson;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            //CommonFunction commonObject = new CommonFunction();
            try
            {
                var UserResponse = await result;

                if ((UserResponse).ToLower() == "cancel" || (UserResponse).ToLower() == "abort" || (UserResponse).ToLower() == "exit")
                {
                    await context.PostAsync(strThank);
                    //PromptDialog.Choice(context, ProcessExit, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    return;
                }
                if (PromptDialogCount == PromptDialogFor.ContactPerson)
                {
                    ContactPersonCount += 1;
                    ContactPerson.Add(UserResponse.ToString());

                    userIntent = IntentSelected.ContactDetails;
                    PromptDialogCount = PromptDialogFor.SchemeName;
                    PromptDialog.Text(context, ProcessContactDetails, "Please provide scheme name ");
                }
                else if (PromptDialogCount == PromptDialogFor.SchemeName)
                {
                    schemeSelected = UserResponse.ToString();
                    if (schemeSelected != "")
                    {
                        SQLQuery = "SELECT * FROM scheme WHERE sch_description LIKE '%" + schemeSelected + "%'";
                        dt = CommonFunction.GetDTForQuery(SQLQuery);

                        if (dt.Rows.Count == 1)
                        {
                            //await context.PostAsync("You you selected the scheme no with ID" + dt.Rows[0]["sch_no"].ToString());
                            optionList.Clear();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                optionList.Add(dt.Rows[i]["sch_description"].ToString());
                            }
                            PromptDialogCount = PromptDialogFor.ConfirmScheme;
                            PromptDialog.Choice(context, ProcessContactDetails, optionList, "Please confirm the scheme name", "Not the valid option", 3, PromptStyle.Auto);
                        }
                        else if (dt.Rows.Count > 1)
                        {
                            optionList.Clear();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                optionList.Add(dt.Rows[i]["sch_description"].ToString());
                            }
                            PromptDialogCount = PromptDialogFor.ConfirmScheme;
                            PromptDialog.Choice(context, ProcessContactDetails, optionList, "We found more then on scheme with name you mention please select your scheme from bellow given option", "Not the valid option", 3, PromptStyle.Auto);
                        }
                        else if (dt.Rows.Count == 0)
                        {
                            PromptDialog.Text(context, ProcessContactDetails, "Please provide the correct scheme name");
                        }
                    }
                    else
                    {
                        PromptDialog.Text(context, ProcessContactDetails, "Please provide the correct scheme name");
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.ConfirmScheme)
                {
                    schemeSelected = UserResponse.ToString();
                    SQLQuery = "SELECT sch_no, sch_description, ContactDetails.* FROM scheme INNER JOIN ContactDetails " +
                                        "ON Scheme.sch_no = ContactDetails.CD_scheme_no " +
                                        "WHERE LOWER(sch_description) = '" + schemeSelected + "'";

                    dt = CommonFunction.GetDTForQuery(SQLQuery);
                    if (dt.Rows.Count == 1)
                    {
                        if (ContactPersonCount != 0)
                        {
                            for (int i = 0; i < ContactPerson.Count; i++)
                            {
                                returnedContactPerson = CommonFunction.returnContactDetails(dt, ContactPerson[i]);
                                if (returnedContactPerson != null && returnedContactPerson != "")
                                {
                                    await context.PostAsync("Contact details of " + ContactPerson[i] + " --> " + returnedContactPerson);
                                    await context.PostAsync(strThank);
                                    //PromptDialog.Choice(context, ProcessContactDetails, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                                }
                                else if (returnedContactPerson == null)
                                {
                                    await context.PostAsync(ContactPerson[i] + " contact person doesn't exist in our records.");
                                }
                                else if (returnedContactPerson == "")
                                {
                                    await context.PostAsync("Sorry we don't have record for the contact person.");
                                }
                            }
                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;
                            //context.Wait(MessageReceived);
                        }
                        else
                        {
                            strMsg = ((dt.Rows[0]["CD_UK_admin"].ToString() == "") ? "" : "UK administrator -> " + dt.Rows[0]["CD_UK_admin"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_UK_senior_Admin"].ToString() == "") ? "" : "UK Scheme Senior Administrator manager -> " + dt.Rows[0]["CD_UK_senior_Admin"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Acturies"].ToString() == "") ? "" : "Scheme Actuary -> " + dt.Rows[0]["CD_Acturies"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Consultant"].ToString() == "") ? "" : "Scheme Consultant -> " + dt.Rows[0]["CD_Consultant"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Owner"].ToString() == "") ? "" : "Scheme owner -> " + dt.Rows[0]["CD_Owner"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Ops_Owner"].ToString() == "") ? "" : "ops owner  -> " + dt.Rows[0]["CD_Ops_Owner"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Client_Manager"].ToString() == "") ? "" : "Client manager -> " + dt.Rows[0]["CD_Client_Manager"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Pensioner_Payroll"].ToString() == "") ? "" : "Pensioner payroll -> " + dt.Rows[0]["CD_Pensioner_Payroll"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Team_Manager"].ToString() == "") ? "" : "Scheme Manager -> " + dt.Rows[0]["CD_Team_Manager"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_PFA"].ToString() == "") ? "" : "PFA -> " + dt.Rows[0]["CD_PFA"].ToString() + "\n\n") +
                                     ((dt.Rows[0]["CD_Admin_Office"].ToString() == "") ? "" : "UK administrator office -> " + dt.Rows[0]["CD_Admin_Office"].ToString());

                            await context.PostAsync(strMsg);
                            await context.PostAsync(strThank);
                            //PromptDialog.Choice(context, ProcessContactDetails, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;
                            // context.Wait(MessageReceived);
                        }
                        //userIntent = IntentSelected.None;
                        //PromptDialogCount = PromptDialogFor.None;
                        //context.Wait(MessageReceived);
                    }
                    else if (dt.Rows.Count > 1)
                    {
                        optionList.Clear();
                        RepeativeCheck = "";
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (RepeativeCheck != dt.Rows[i]["CD_category_name"] + "_" + dt.Rows[i]["CD_sub_category_name"])
                            {
                                RepeativeCheck = dt.Rows[i]["CD_category_name"] + "_" + dt.Rows[i]["CD_sub_category_name"];
                                optionList.Add(dt.Rows[i]["CD_category_name"].ToString() +
                                    (dt.Rows[i]["CD_sub_category_name"].ToString() != "" ?
                                    "Sub Category:" + dt.Rows[i]["CD_sub_category_name"].ToString() : ""));
                            }

                        }
                        PromptDialogCount = PromptDialogFor.CategoryName;
                        PromptDialog.Choice(context, ProcessContactDetails, optionList, "the provided scheme have more then one category, Please Select the category", "Not the valid option", 3, PromptStyle.Auto);
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.CategoryName)
                {
                    SchemeCategory = UserResponse.ToString();
                    SchemeOldCategory = "";

                    if (SchemeCategory.Contains("Sub Category:"))
                    {
                        SchemeOldCategory = SchemeCategory.Substring(SchemeCategory.IndexOf("Sub Category:") + "Sub Category:".Length,
                                                SchemeCategory.Length - ((SchemeCategory.IndexOf("Sub Category:") + "Sub Category:".Length)));
                        SchemeCategory = SchemeCategory.Substring(0, SchemeCategory.IndexOf("Sub Category:"));
                    }

                    SQLQuery = "SELECT sch_no, sch_description, ContactDetails.* from Scheme INNERJ JOIN ContactDetails ON sch_no = CD_scheme_no " +
                            "WHERE LOWER(sch_description) = '" + schemeSelected + "' AND CD_category_name = '" + SchemeCategory +
                            "'" + (SchemeOldCategory != "" ? " AND CD_sub_category_name = '" + SchemeOldCategory + "'" : "");

                    dt = CommonFunction.GetDTForQuery(SQLQuery);

                    if (ContactPersonCount != 0)
                    {
                        for (int i = 0; i < ContactPerson.Count; i++)
                        {
                            returnedContactPerson = CommonFunction.returnContactDetails(dt, ContactPerson[i]);
                            if (returnedContactPerson != null && returnedContactPerson != "")
                            {
                                await context.PostAsync(ContactPerson[i] + " : " + returnedContactPerson);
                                await context.PostAsync(strThank);
                                //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                            }
                            else if (returnedContactPerson == null)
                            {
                                await context.PostAsync(ContactPerson[i] + " contact person doesn't exist in our records.");
                            }
                            else if (returnedContactPerson == "")
                            {
                                await context.PostAsync("Sorry we dont have record for the contact person.");
                            }
                        }
                        userIntent = IntentSelected.None;
                        PromptDialogCount = PromptDialogFor.None;
                        //context.Wait(MessageReceived);
                    }
                    else
                    {
                        strMsg = ((dt.Rows[0]["CD_UK_admin"].ToString() == "") ? "" : "UK administrator -> " + dt.Rows[0]["CD_UK_admin"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_UK_senior_Admin"].ToString() == "") ? "" : "UK Scheme Senior Administrator manager -> " + dt.Rows[0]["CD_UK_senior_Admin"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Acturies"].ToString() == "") ? "" : "Scheme Actuary -> " + dt.Rows[0]["CD_Acturies"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Consultant"].ToString() == "") ? "" : "Scheme Consultant -> " + dt.Rows[0]["CD_Consultant"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Owner"].ToString() == "") ? "" : "Scheme owner -> " + dt.Rows[0]["CD_Owner"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Ops_Owner"].ToString() == "") ? "" : "ops owner  -> " + dt.Rows[0]["CD_Ops_Owner"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Client_Manager"].ToString() == "") ? "" : "Client manager -> " + dt.Rows[0]["CD_Client_Manager"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Pensioner_Payroll"].ToString() == "") ? "" : "Pensioner payroll -> " + dt.Rows[0]["CD_Pensioner_Payroll"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Team_Manager"].ToString() == "") ? "" : "Scheme Manager -> " + dt.Rows[0]["CD_Team_Manager"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_PFA"].ToString() == "") ? "" : "PFA -> " + dt.Rows[0]["CD_PFA"].ToString() + "\n\n") +
                                 ((dt.Rows[0]["CD_Admin_Office"].ToString() == "") ? "" : "UK administrator office -> " + dt.Rows[0]["CD_Admin_Office"].ToString());

                        await context.PostAsync(strMsg);
                        await context.PostAsync(strThank);
                        //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                        userIntent = IntentSelected.None;
                        PromptDialogCount = PromptDialogFor.None;
                        //context.Wait(MessageReceived);
                    }
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    //context.Wait(MessageReceived);
                }
                else
                {
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    if (UserResponse.ToString().ToLower() == "no")
                    {
                        await context.PostAsync("Sorry to hear that I could not be of more help.\n\n In future, feel free to contact.");
                        await context.PostAsync("Have a nice day!");
                    }
                    else
                    {
                        await context.PostAsync("How can I help you?");
                    }
                    context.Wait(MessageReceived);
                }
            }
            catch (Exception ex)
            {
                await context.PostAsync(ex.Message);
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("TrivialCommutationFactor")]
        public async Task GetTVCFactor(IDialogContext context, LuisResult result)
        {
            schemeSelected = "";
            strAge = "";
            ageyears = 0;
            ageMonths = 0;

            string SQLQuery;
            string RepeativeCheck;
            string returnedContactPerson;
            int nmonthStartLocation;
            int nmonthEndLocation;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();

            try
            {
                if (userIntent == IntentSelected.None || userIntent == IntentSelected.TVCFactor)
                {
                    var entities = new List<EntityRecommendation>(result.Entities);
                    string UserQuery = result.Query;
                    schemeSelected = "";

                    if (entities.Count == 0)
                    {
                        userIntent = IntentSelected.TVCFactor;
                        PromptDialogCount = PromptDialogFor.SchemeName;
                        PromptDialog.Text(context, ProcessTVCFactor, "Provide scheme name ");
                    }
                    else
                    {
                        foreach (var entity in entities)
                        {
                            if (entity.Type == "Scheme")
                            {
                                schemeSelected = entity.Entity;
                            }
                            else if (entity.Type == "ProformaAge")
                            {
                                strAge = entity.Entity;
                            }
                        }


                        if (schemeSelected != "")
                        {
                            SQLQuery = "SELECT sch_no, sch_description FROM scheme " +
                                        "WHERE LOWER(sch_description) = '" + schemeSelected + "'";

                            dt = CommonFunction.GetDTForQuery(SQLQuery);

                            if (dt.Rows.Count == 0)
                            {
                                userIntent = IntentSelected.TVCFactor;
                                PromptDialogCount = PromptDialogFor.SchemeName;
                                PromptDialog.Text(context, ProcessTVCFactor, "Provide correct scheme name ");
                            }
                            else
                            {
                                SQLQuery = "SELECT sch_no, sch_description, Trival_Comm_Fact.* FROM scheme INNER JOIN Trival_Comm_Fact " +
                                        "ON Scheme.sch_no = Trival_Comm_Fact.TCF_scheme_no " +
                                        "WHERE LOWER(sch_description) = '" + schemeSelected + "'";

                                dt = CommonFunction.GetDTForQuery(SQLQuery);

                                if (dt.Rows.Count > 1)
                                {
                                    if (strAge != "")
                                    {
                                        try
                                        {
                                            if (Regex.IsMatch(strAge.Replace(" ",""), @"\d{2}(y|yy|years|year)(\d[0-12](m|mm|month|months))?"))
                                            {
                                                ageyears = Convert.ToInt32(Regex.Match(strAge.Replace(" ",""), @"\d+", RegexOptions.None).Value);
                                                ageMonths = Convert.ToInt32(Regex.Match(strAge.Replace(" ",""), @"\d+", RegexOptions.RightToLeft).Value);
                                                if (ageyears == ageMonths)
                                                {
                                                    ageMonths = 0;
                                                }
                                                //ageyears = Convert.ToInt32(strAge.Substring(0, 2));
                                                //ageMonths = 0;
                                                //nmonthEndLocation = strAge.IndexOf("month") - 2;
                                                //nmonthStartLocation = 0;
                                                //if (nmonthEndLocation > 0)
                                                //{
                                                //    for (int i = nmonthEndLocation - 1; i > 0; i--)
                                                //    {
                                                //        if (strAge[i] == ' ')
                                                //        {
                                                //            nmonthStartLocation = i + 1;
                                                //            break;
                                                //        }
                                                //    }
                                                //    ageMonths = Convert.ToInt32(strAge.Substring(nmonthStartLocation, (nmonthEndLocation - nmonthStartLocation + 1)));
                                                //}

                                                //await context.PostAsync("Correct Age " + ageyears + " " + ageMonths);
                                                string strOutput = string.Empty;

                                                SQLQuery = "SELECT sch_no, sch_description, Trival_Comm_Fact.* FROM scheme INNER JOIN Trival_Comm_Fact " +
                                                    "ON Scheme.sch_no = Trival_Comm_Fact.TCF_scheme_no " +
                                                    "WHERE LOWER(sch_description) = '" + schemeSelected + "'" +
                                                    "AND TCF_age IN (" + ageyears + (ageMonths == 0 ? ")" : ", " + (ageyears + 1) + ")");
                                                dt = CommonFunction.GetDTForQuery(SQLQuery);
                                                if (dt.Rows.Count > 0)
                                                {
                                                    for (int i = 0; i < dt.Rows.Count; i++)
                                                    {
                                                        strOutput += "Age " + (ageyears + i) + " -> " + " TCF value : " + dt.Rows[i]["TCF_comm_fact"] + "\n\n";
                                                    }
                                                    if (ageMonths != 0)
                                                    {
                                                        strOutput = strOutput + "\n Interpolated value ->" + CommonFunction.interpolated_TVCFactor(ageyears, ageMonths,
                                                            float.Parse(dt.Rows[0]["TCF_comm_fact"].ToString()),
                                                            float.Parse(dt.Rows[1]["TCF_comm_fact"].ToString()));
                                                    }
                                                }
                                                else
                                                {
                                                    strOutput = "Sorry, no record exists with provided input.";
                                                }
                                                await context.PostAsync(strOutput);

                                                userIntent = IntentSelected.None;
                                                PromptDialogCount = PromptDialogFor.None;
                                                context.Wait(MessageReceived);
                                            }
                                            else
                                            {
                                                userIntent = IntentSelected.TVCFactor;
                                                PromptDialogCount = PromptDialogFor.TVCAge;
                                                PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                                            }
                                        }
                                        catch (FormatException)
                                        {
                                            userIntent = IntentSelected.TVCFactor;
                                            PromptDialogCount = PromptDialogFor.TVCAge;
                                            PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                                        }
                                    }
                                    else
                                    {
                                        userIntent = IntentSelected.TVCFactor;
                                        PromptDialogCount = PromptDialogFor.TVCAge;
                                        PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                                    }
                                }
                                else
                                {
                                    userIntent = IntentSelected.None;
                                    PromptDialogCount = PromptDialogFor.None;
                                    await context.PostAsync("TCF value --> " + dt.Rows[0]["TCF_comm_fact"].ToString());
                                    await context.PostAsync(strThank);
                                    //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                                    //context.Wait(MessageReceived);
                                }
                            }
                        }
                        else
                        {
                            userIntent = IntentSelected.TVCFactor;
                            PromptDialogCount = PromptDialogFor.SchemeName;
                            PromptDialog.Text(context, ProcessTVCFactor, "Provide correct scheme name ");
                        }

                    }
                }
                else
                {
                    await context.PostAsync("Please cancel the previous operation");
                    context.Wait(MessageReceived);
                }
            }
            catch (Exception ex)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync(ex.Message);
                context.Wait(MessageReceived);
            }
        }

        public async Task ProcessTVCFactor(IDialogContext context, IAwaitable<string> result)
        {
            string SQLQuery;
            string RepeativeCheck;
            string returnedContactPerson;
            int nmonthStartLocation;
            int nmonthEndLocation;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            //CommonFunction commonObject = new CommonFunction();
            try
            {
                var UserResponse = await result;

                if ((UserResponse).ToLower() == "cancel" || (UserResponse).ToLower() == "abort" || (UserResponse).ToLower() == "exit")
                {
                    await context.PostAsync(strThank);
                    //PromptDialog.Choice(context, ProcessExit, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    return;
                }

                if (PromptDialogCount == PromptDialogFor.SchemeName)
                {
                    schemeSelected = UserResponse.ToString();
                    if (schemeSelected != "")
                    {
                        SQLQuery = "SELECT * FROM scheme WHERE sch_description LIKE '%" + schemeSelected + "%'";
                        dt = CommonFunction.GetDTForQuery(SQLQuery);

                        if (dt.Rows.Count == 1)
                        {
                            //await context.PostAsync("You you selected the scheme no with ID" + dt.Rows[0]["sch_no"].ToString());
                            optionList.Clear();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                optionList.Add(dt.Rows[i]["sch_description"].ToString());
                            }
                            PromptDialogCount = PromptDialogFor.ConfirmScheme;
                            PromptDialog.Choice(context, ProcessTVCFactor, optionList, "Please confirm the scheme name", "Not the valid option", 3, PromptStyle.Auto);
                        }
                        else if (dt.Rows.Count > 1)
                        {
                            optionList.Clear();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                optionList.Add(dt.Rows[i]["sch_description"].ToString());
                            }
                            PromptDialogCount = PromptDialogFor.ConfirmScheme;
                            PromptDialog.Choice(context, ProcessTVCFactor, optionList, "We found more then on scheme with name you mention please select your scheme from bellow given option", "Not the valid option", 3, PromptStyle.Auto);
                        }
                        else if (dt.Rows.Count == 0)
                        {
                            PromptDialog.Text(context, ProcessTVCFactor, "Please provide the correct scheme name");
                        }
                    }
                    else
                    {
                        PromptDialog.Text(context, ProcessTVCFactor, "Please provide the correct scheme name");
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.ConfirmScheme)
                {
                    schemeSelected = UserResponse.ToString();
                    SQLQuery = "SELECT sch_no, sch_description, Trival_Comm_Fact.* FROM scheme INNER JOIN Trival_Comm_Fact " +
                                        "ON Scheme.sch_no = Trival_Comm_Fact.TCF_scheme_no " +
                                        "WHERE LOWER(sch_description) = '" + schemeSelected + "'";

                    dt = CommonFunction.GetDTForQuery(SQLQuery);

                    if (dt.Rows.Count > 1)
                    {
                        if (strAge != "")
                        {
                            try
                            {
                                if (Regex.IsMatch(strAge.Replace(" ",""), @"\d{2}(y|yy|years|year)(\d[0-12](m|mm|month|months))?"))
                                {
                                    ageyears = Convert.ToInt32(Regex.Match(strAge.Replace(" ",""), @"\d+", RegexOptions.None).Value);
                                    ageMonths = Convert.ToInt32(Regex.Match(strAge.Replace(" ",""), @"\d+", RegexOptions.RightToLeft).Value);
                                    if (ageyears == ageMonths)
                                    {
                                        ageMonths = 0;
                                    }
                                    //ageyears = Convert.ToInt32(strAge.Substring(0, 2));
                                    //ageMonths = 0;
                                    //nmonthEndLocation = strAge.IndexOf("month") - 2;
                                    //nmonthStartLocation = 0;
                                    //if (nmonthEndLocation > 0)
                                    //{
                                    //    for (int i = nmonthEndLocation - 1; i > 0; i--)
                                    //    {
                                    //        if (strAge[i] == ' ')
                                    //        {
                                    //            nmonthStartLocation = i + 1;
                                    //            break;
                                    //        }
                                    //    }
                                    //    ageMonths = Convert.ToInt32(strAge.Substring(nmonthStartLocation, (nmonthEndLocation - nmonthStartLocation + 1)));
                                    //}

                                    //await context.PostAsync("Correct Age " + ageyears + " " + ageMonths);
                                    string strOutput = string.Empty;

                                    SQLQuery = "SELECT sch_no, sch_description, Trival_Comm_Fact.* FROM scheme INNER JOIN Trival_Comm_Fact " +
                                        "ON Scheme.sch_no = Trival_Comm_Fact.TCF_scheme_no " +
                                        "WHERE LOWER(sch_description) = '" + schemeSelected + "'" +
                                        "AND TCF_age IN (" + ageyears + (ageMonths == 0 ? ")" : ", " + (ageyears + 1) + ")");
                                    dt = CommonFunction.GetDTForQuery(SQLQuery);
                                    if (dt.Rows.Count > 0)
                                    {
                                        for (int i = 0; i < dt.Rows.Count; i++)
                                        {
                                            strOutput += "Age " + (ageyears + i) + " -> " + " TCF value : " + dt.Rows[i]["TCF_comm_fact"] + "\n\n";
                                        }
                                        if (ageMonths != 0)
                                        {
                                            strOutput = strOutput + "\n Interpolated value ->" + CommonFunction.interpolated_TVCFactor(ageyears, ageMonths,
                                                float.Parse(dt.Rows[0]["TCF_comm_fact"].ToString()),
                                                float.Parse(dt.Rows[1]["TCF_comm_fact"].ToString()));
                                        }
                                    }
                                    else
                                    {
                                        strOutput = "Sorry, no record exists with provided input.";
                                    }
                                    await context.PostAsync(strOutput);
                                    await context.PostAsync(strThank);
                                    //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                                    userIntent = IntentSelected.None;
                                    PromptDialogCount = PromptDialogFor.None;
                                    //context.Wait(MessageReceived);
                                }
                                else
                                {
                                    userIntent = IntentSelected.TVCFactor;
                                    PromptDialogCount = PromptDialogFor.TVCAge;
                                    PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                                }
                            }
                            catch (FormatException)
                            {
                                userIntent = IntentSelected.TVCFactor;
                                PromptDialogCount = PromptDialogFor.TVCAge;
                                PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                            }
                        }
                        else
                        {
                            userIntent = IntentSelected.TVCFactor;
                            PromptDialogCount = PromptDialogFor.TVCAge;
                            PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                        }
                    }
                    else
                    {
                        userIntent = IntentSelected.None;
                        PromptDialogCount = PromptDialogFor.None;
                        await context.PostAsync("TCF value --> " + dt.Rows[0]["TCF_comm_fact"].ToString());
                        await context.PostAsync(strThank);
                        //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                        //context.Wait(MessageReceived);
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.TVCAge)
                {
                    strAge = UserResponse.ToString();

                    try
                    {
                        if (Regex.IsMatch(strAge.Replace(" ",""), @"\d{2}(y|yy|years|year)(\d[0-12](m|mm|month|months))?"))
                        {
                            ageyears = Convert.ToInt32(Regex.Match(strAge.Replace(" ",""), @"\d+", RegexOptions.None).Value);
                            ageMonths = Convert.ToInt32(Regex.Match(strAge.Replace(" ",""), @"\d+", RegexOptions.RightToLeft).Value);
                            if (ageyears == ageMonths)
                            {
                                ageMonths = 0;
                            }
                            //ageyears = Convert.ToInt32(strAge.Substring(0, 2));
                            //ageMonths = 0;
                            //nmonthEndLocation = strAge.IndexOf("month") - 2;
                            //nmonthStartLocation = 0;
                            //if (nmonthEndLocation > 0)
                            //{
                            //    for (int i = nmonthEndLocation - 1; i > 0; i--)
                            //    {
                            //        if (strAge[i] == ' ')
                            //        {
                            //            nmonthStartLocation = i + 1;
                            //            break;
                            //        }
                            //    }
                            //    ageMonths = Convert.ToInt32(strAge.Substring(nmonthStartLocation, (nmonthEndLocation - nmonthStartLocation + 1)));
                            //}

                            //await context.PostAsync("Correct Age " + ageyears + " " + ageMonths);
                            string strOutput = string.Empty;
                            SQLQuery = "SELECT sch_no, sch_description, Trival_Comm_Fact.* FROM scheme INNER JOIN Trival_Comm_Fact " +
                                "ON Scheme.sch_no = Trival_Comm_Fact.TCF_scheme_no " +
                                "WHERE LOWER(sch_description) = '" + schemeSelected + "'" +
                                "AND TCF_age IN (" + ageyears + (ageMonths == 0 ? ")" : ", " + (ageyears + 1) + ")");
                            dt = CommonFunction.GetDTForQuery(SQLQuery);
                            if (dt.Rows.Count > 0)
                            {
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    strOutput += "Age " + (ageyears + i) + " -> " + " TCF value : " + dt.Rows[i]["TCF_comm_fact"] + "\n\n";
                                }
                                if (ageMonths != 0)
                                {
                                    strOutput = strOutput + "\n Interpolated value ->" + CommonFunction.interpolated_TVCFactor(ageyears, ageMonths,
                                        float.Parse(dt.Rows[0]["TCF_comm_fact"].ToString()),
                                        float.Parse(dt.Rows[1]["TCF_comm_fact"].ToString()));
                                }
                            }
                            else
                            {
                                strOutput = "Sorry, no record exists with provided input.";
                            }
                            await context.PostAsync(strOutput);
                            await context.PostAsync(strThank);
                            //PromptDialog.Choice(context, ProcessAccrualRate, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;
                            //context.Wait(MessageReceived);
                        }
                        else
                        {
                            userIntent = IntentSelected.TVCFactor;
                            PromptDialogCount = PromptDialogFor.TVCAge;
                            PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected formats - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                        }
                    }
                    catch (FormatException)
                    {
                        userIntent = IntentSelected.TVCFactor;
                        PromptDialogCount = PromptDialogFor.TVCAge;
                        PromptDialog.Text(context, ProcessTVCFactor, "Please provide the age satisfying below criterias :\n\n 1. Age should be greater than or equal to 55 years." +
                                            "\n\n 2. Expected format examples - 55y3m, 55yy3mm, 55 year 3 month, 55 years 3 months");
                    }
                }
                else
                {
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    if (UserResponse.ToString().ToLower() == "no")
                    {
                        await context.PostAsync("Sorry to hear that I could not be of more help.\n\n In future, feel free to contact.");
                        await context.PostAsync("Have a nice day!");
                    }
                    else
                    {
                        await context.PostAsync("How can I help you?");
                    }
                    context.Wait(MessageReceived);
                }
            }
            catch (Exception ex)
            {
                //TVC START
                await context.PostAsync(ex.Message);
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                context.Wait(MessageReceived);
                //TVC END
            }
        }

        [LuisIntent("None")]
        [LuisIntent("")]
        public async Task GetSchemeName(IDialogContext context, LuisResult result)
        {
            if ((result.Query).ToLower().Contains("help"))
            {
                //PromptDialog.Choice(context, ProcessExit, new List<string> { "Accrual Rate", "Contact details", "Trivial Commutation Factor" }, "Help guide :\n\n I highly encourage that you specify clear intents.", "Not the valid option", 3, PromptStyle.Auto);
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync("Help guide :\n\n I highly encourage that you specify clear intents like,\n\n1. Accrual Rate @1\n\n2. Contact details @2\n\n3. Trivial Commutation Factor @3");
                context.Wait(MessageReceived);
            }
            else if ((result.Query).ToLower() == "cancel" || (result.Query).ToLower() == "abort" || (result.Query).ToLower() == "exit")
            {
                await context.PostAsync(strThank);
                //PromptDialog.Choice(context, ProcessExit, new List<string> { "Yes", "No" }, "Is there anything else I can help you with?", "not the valid option", 3, PromptStyle.Auto);
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
            }
            else
            {
                await context.PostAsync("Sorry I am not able to understand you. \n\n Can you please reframe your question?");
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("TellName")]
        public async Task TellName(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Well, I am Genie!");
            context.Wait(MessageReceived);
        }

        [LuisIntent("TellWork")]
        public async Task TellWork(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I help people by solving their queries related to Proforma.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Shortcut")]
        public async Task GetShortcut(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Shortcuts : \n\n1. Accrual Rate – AR, Acc Rate. \n\n2. Contact Details – CD, Contact, Con Dets.\n\n3. Trivial Commutation Factor – TVC, TCF, Triv Comm Fact, TVC factors.");
            context.Wait(MessageReceived);
        }
        //public async Task ProcessExit(IDialogContext context, IAwaitable<string> result)
        //{
        //var UserResponse = await result;

        //userIntent = IntentSelected.None;
        //PromptDialogCount = PromptDialogFor.None;
        //if (UserResponse.ToString().ToLower() == "no")
        //{
        //    await context.PostAsync("Sorry to hear that I could not be of more help.\n\n In future, feel free to contact.");
        //    await context.PostAsync("Have a nice day!");
        //}
        //else
        //{
        //    await context.PostAsync("How can I help you?");
        //}
        //context.Wait(MessageReceived);
        //}
    }
}