using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Bot_Application1.Dialogs
{
    [LuisModel("8632cb36-46ae-450a-9fe8-8f2ed9b3f74f", "6c97936f267a4fffa8b935058307adba", LuisApiVersion.V2)]
    [Serializable]
    class profomaDialog : LuisDialog<profomaOption>
    {

        string schemeSelected;
        bool userIntentset;
        string SchemeOldCategory;
        string SchemeCategory;
        string SchemeSubCategory;
        int ContactPersonCount;
        List<string> ContactPerson;
        
        enum IntentSelected
        {
            None,
            MinimumAge,
            maximumAge,
            AccrualRates,
            LowerAccrualRates,
            MiddleAccrualRates,
            UpperAccrualRates,
            ContactDetails
        };
        enum PromptDialogFor
        {
            None,
            SchemeName,
            CategoryName,
            SubCategoryName,
            OldCategory,
            ConfirmScheme
        };

        PromptDialogFor PromptDialogCount;
        List<String> AccrualType = new List<string>();
        
        IntentSelected userIntent = IntentSelected.None;
        public async Task StartAsync(IDialogContext context)
        {
            
            context.Wait(MessageReceived);

            /*return Task.CompletedTask;*/
        }

        [LuisIntent("hello")]
        public async Task Greet(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi, how can I help you");
            context.Wait(MessageReceived);
        }

        [LuisIntent("MinimumAge")]
        
        public async Task GetSchemeMinimumAge(IDialogContext context, LuisResult result)
        {
            var entities = new List<EntityRecommendation>(result.Entities);
            string UserQuery = result.Query;

            if(entities.Count !=0)
            {
                foreach (var entity in result.Entities)
                {
                    if (entity.Type == "scheme")
                    {
                        schemeSelected = UserQuery.Substring((int)entity.StartIndex, (int)(entity.EndIndex - entity.StartIndex + 1));
                        if(schemeSelected.ToLower() != "scheme")
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
        public async Task GetAccrualRate(IDialogContext context,LuisResult result)
        {
            schemeSelected = "";
            SchemeCategory = "";
            SchemeSubCategory = "";
            SchemeOldCategory = "";
            string SQLQuery;
            string RepeativeCheck;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            CommonFunction commonObject = new CommonFunction();

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
                        PromptDialog.Text(context, ProcessAccrualRate, "Please Provide the scheme name");
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


                        await context.PostAsync(schemeSelected + "\n\n" + SchemeCategory);

                        SQLQuery = "select * from Scheme INNER JOIN AccrualRate ON acr_schemeNo = sch_no " +
                                "where LOWER(sch_description) = '" + schemeSelected + "' " +
                                (SchemeCategory == "" ? "" : "and LOWER(acr_catagoryDescription) = '" + SchemeCategory + "'");
                        dt = commonObject.GetDTForQuery(SQLQuery);

                        if(dt.Rows.Count == 0)
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
                        else if(dt.Rows.Count > 1)
                        {
                            if(SchemeCategory == "")
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
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "the provided scheme have more then one category, Please Select the category", "not the valid option", 3, PromptStyle.Auto);
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

                                if(optionList.Count != 0)
                                {
                                    PromptDialogCount = PromptDialogFor.CategoryName;
                                    PromptDialog.Choice(context, ProcessAccrualRate, optionList, "the provided scheme have more then one historic category, Please Select the revelent category", "not the valid option", 3, PromptStyle.Auto);
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
                                    PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please Select the sub category", "not the valid option", 3, PromptStyle.Auto);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }
            

        }

        public async Task ProcessAccrualRate(IDialogContext context,IAwaitable<string> result)
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
                CommonFunction commonObject = new CommonFunction();

                var UserResponse = await result;
                if (PromptDialogCount == PromptDialogFor.SchemeName || PromptDialogCount == PromptDialogFor.ConfirmScheme)
                {
                    if (PromptDialogCount == PromptDialogFor.SchemeName)
                    {
                        schemeSelected = UserResponse.ToString();
                        if (schemeSelected != "")
                        {
                            SQLQuery = "SELECT * FROM scheme WHERE sch_description LIKE '%" + schemeSelected + "%'";
                            dt = commonObject.GetDTForQuery(SQLQuery);

                            if (dt.Rows.Count == 1)
                            {
                                //await context.PostAsync("You you selected the scheme no with ID" + dt.Rows[0]["sch_no"].ToString());
                                optionList.Clear();
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    optionList.Add(dt.Rows[i]["sch_description"].ToString());
                                }
                                PromptDialogCount = PromptDialogFor.ConfirmScheme;
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "please confirm the scheme name", "not the valid option", 3, PromptStyle.Auto);
                            }
                            else if(dt.Rows.Count > 1)
                            {
                                optionList.Clear();
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    optionList.Add(dt.Rows[i]["sch_description"].ToString());
                                }
                                PromptDialogCount = PromptDialogFor.ConfirmScheme;
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "We found more then on scheme with name you mention please select your scheme from bellow given option", "not the valid option", 3, PromptStyle.Auto);
                            }
                            else if(dt.Rows.Count == 0)
                            {
                                PromptDialog.Text(context, ProcessAccrualRate, "Please Provide the correct scheme name");
                            }                           
                        }
                        else
                        {
                            PromptDialog.Text(context, ProcessAccrualRate, "Please Provide the correct scheme name");
                        }
                    }      
                    else if (PromptDialogCount == PromptDialogFor.ConfirmScheme)
                    {
                        schemeSelected = UserResponse.ToString();

                        //check for category
                        SQLQuery = "SELECT sch_no FROM Scheme WHERE sch_description = '" + schemeSelected + "'";
                        
                        string schemeID = commonObject.GetSingleSQLRecord(SQLQuery);
                        string HasDifferentAccrual = commonObject.GetSingleSQLRecord("SELECT sch_hasDifferentAccrualRate FROM Scheme WHERE sch_description = '" + schemeSelected + "'");

                        if (HasDifferentAccrual == "Y")
                        {
                            SQLQuery = "Select * from AccrualRate Where acr_schemeNo = " + schemeID;

                                
                            dt = commonObject.GetDTForQuery(SQLQuery);

                            if (dt.Rows.Count == 1)
                            {
                                userIntent = IntentSelected.None;
                                PromptDialogCount = PromptDialogFor.None;
                                await context.PostAsync("The Accrual Rate of '" + schemeSelected + "' is " + dt.Rows[0]["acr_accrualRateValue"].ToString());
                                context.Wait(MessageReceived);
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
                                PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please Select the category", "not the valid option", 3, PromptStyle.Auto);
                            }
                        }
                        else
                        {
                            SQLQuery = "Select Top 1 acr_accrualRateValue from AccrualRate Where acr_schemeNo = " + schemeID;

                            string accrualRate = commonObject.GetSingleSQLRecord(SQLQuery);

                            userIntent = IntentSelected.None;
                            PromptDialogCount = PromptDialogFor.None;

                            await context.PostAsync("The Accrual Rate of scheme '" + schemeSelected + "is " + accrualRate);
                            context.Wait(MessageReceived);
                        }   
                    }     
                }
                else if(PromptDialogCount == PromptDialogFor.CategoryName)
                {
                    SchemeCategory = UserResponse;
                    if(SchemeCategory.Contains("Old Category:"))
                    {
                        SchemeOldCategory = SchemeCategory.Substring(SchemeCategory.IndexOf("Old Category:") + "Old Category:".Length,
                                                SchemeCategory.Length -((SchemeCategory.IndexOf("Old Category:") + "Old Category:".Length)));
                        SchemeCategory = SchemeCategory.Substring(0, SchemeCategory.IndexOf("Old Category:"));
                    }

                    //await context.PostAsync("Your Category " + SchemeCategory);
                    //await context.PostAsync("Your category " + SchemeOldCategory);

                    SQLQuery = "SELECT * FROM AccrualRate WHERE acr_catagoryDescription = '" + SchemeCategory + "' " +
                        (SchemeOldCategory == "" || SchemeOldCategory == null? "" : "AND acr_PreviousCategoryDescription = '" + SchemeOldCategory + "'");
                   
                    dt = new CommonFunction().GetDTForQuery(SQLQuery);

                    if(dt.Rows.Count == 1)
                    {
                        userIntent = IntentSelected.None;
                        PromptDialogCount = PromptDialogFor.None;
                        await context.PostAsync("The Accrual Rate of scheme '" + schemeSelected + "' \n with category '" +
                            SchemeCategory + "' " + (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : "\n And scheme old Category '" + SchemeOldCategory + "'") +
                            "is " + dt.Rows[0]["acr_accrualRateValue"].ToString());
                        context.Wait(MessageReceived);
                    }
                    else if(dt.Rows.Count > 1)
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
                        PromptDialog.Choice(context, ProcessAccrualRate, optionList, "Please Select the sub category", "not the valid option", 3, PromptStyle.Auto);
                    }     
                }
                else if(PromptDialogCount == PromptDialogFor.SubCategoryName)
                {
                    SchemeSubCategory = UserResponse.ToString();

                    SQLQuery = "SELECT acr_accrualRateValue FROM AccrualRate WHERE acr_catagoryDescription = '" + SchemeCategory + "' " +
                        (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : "AND acr_PreviousCategoryDescription = '" + SchemeOldCategory + "'") +
                        "AND acr_subCategory = '" + SchemeSubCategory + "'";
 
                    AccrualRateNo = new CommonFunction().GetSingleSQLRecord(SQLQuery);

                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    await context.PostAsync("The Accrual Rate of scheme '" + schemeSelected + "' \n\n with category '" +
                            SchemeCategory + "' " + (SchemeOldCategory == "" || SchemeOldCategory == null ? "" : "\n\n And scheme old Category '" + SchemeOldCategory + "'") +
                            "\n\n and sub Category " + SchemeSubCategory + " is " + AccrualRateNo);
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
            catch(TooManyAttemptsException)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync("You have Exausted the Attempt chances");
                context.Wait(MessageReceived);
            }
            catch(Exception ex)
            {
                userIntent = IntentSelected.None;
                PromptDialogCount = PromptDialogFor.None;
                await context.PostAsync(ex.Message);
                context.Wait(MessageReceived);
            }

            
        }

        [LuisIntent("ContactDetails")]
        public async Task GetContactDetails(IDialogContext context,LuisResult result)
        {
            schemeSelected = "";
            ContactPersonCount = 0;
            ContactPerson = new List<string>();
            
            string SQLQuery;
            string RepeativeCheck;
            string returnedContactPerson;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            CommonFunction commonObject = new CommonFunction();

            try
            {
                if(userIntent == IntentSelected.None || userIntent == IntentSelected.ContactDetails)
                {
                    var entities = new List<EntityRecommendation>(result.Entities);
                    string UserQuery = result.Query;
                    schemeSelected = "";

                    if (entities.Count == 0)
                    {
                        //userIntent = IntentSelected.AccrualRates;
                        //PromptDialogCount = PromptDialogFor.SchemeName;
                        //PromptDialog.Text(context, ProcessAccrualRate, "Please Provide the scheme name");
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

                        if(ContactPersonCount != 0 && schemeSelected != "")
                        {
                            SQLQuery = "SELECT * FROM scheme WHERE LOWER(sch_description) = '" + schemeSelected + "'";
                            dt = commonObject.GetDTForQuery(SQLQuery);

                            if(dt.Rows.Count == 0)
                            {
                                userIntent = IntentSelected.ContactDetails;
                                PromptDialogCount = PromptDialogFor.SchemeName;
                                PromptDialog.Text(context, ProcessContactDetails, "Provide correct scheme name ");
                            }
                            else
                            {
                                for(int i = 0;i < ContactPerson.Count; i++)
                                {
                                    returnedContactPerson = commonObject.returnContactDetails(dt, ContactPerson[i]);
                                    if(returnedContactPerson != null && returnedContactPerson != "")
                                    {
                                        await context.PostAsync(ContactPerson[i] + " : " + returnedContactPerson);
                                    }
                                    else if(returnedContactPerson == null)
                                    {
                                        await context.PostAsync(ContactPerson[i] + " doesnt exist, or try again later");
                                    }
                                    else if(returnedContactPerson == "")
                                    {
                                        await context.PostAsync("Sorry we dont have record for " + ContactPerson[i]);
                                    }
                                }
                                userIntent = IntentSelected.None;
                                PromptDialogCount = PromptDialogFor.None;
                                context.Wait(MessageReceived);
                            }
                        }
                        if(ContactPersonCount !=0 && schemeSelected == "")
                        {
                            userIntent = IntentSelected.ContactDetails;
                            PromptDialogCount = PromptDialogFor.SchemeName;
                            PromptDialog.Text(context, ProcessContactDetails, "Provide correct scheme name ");
                        }
                        if(ContactPersonCount == 0)
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
            schemeSelected = "";
            
            string SQLQuery;
            string RepeativeCheck;
            string returnedContactPerson;
            DataTable dt = new DataTable();
            List<string> optionList = new List<string>();
            CommonFunction commonObject = new CommonFunction();
            try
            {
                var UserResponse = await result;
                if (PromptDialogCount == PromptDialogFor.SchemeName)
                {
                    schemeSelected = UserResponse.ToString();
                    if (schemeSelected != "")
                    {
                        SQLQuery = "SELECT * FROM scheme WHERE sch_description LIKE '%" + schemeSelected + "%'";
                        dt = commonObject.GetDTForQuery(SQLQuery);

                        if (dt.Rows.Count == 1)
                        {
                            //await context.PostAsync("You you selected the scheme no with ID" + dt.Rows[0]["sch_no"].ToString());
                            optionList.Clear();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                optionList.Add(dt.Rows[i]["sch_description"].ToString());
                            }
                            PromptDialogCount = PromptDialogFor.ConfirmScheme;
                            PromptDialog.Choice(context, ProcessContactDetails, optionList, "please confirm the scheme name", "not the valid option", 3, PromptStyle.Auto);
                        }
                        else if (dt.Rows.Count > 1)
                        {
                            optionList.Clear();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                optionList.Add(dt.Rows[i]["sch_description"].ToString());
                            }
                            PromptDialogCount = PromptDialogFor.ConfirmScheme;
                            PromptDialog.Choice(context, ProcessContactDetails, optionList, "We found more then on scheme with name you mention please select your scheme from bellow given option", "not the valid option", 3, PromptStyle.Auto);
                        }
                        else if (dt.Rows.Count == 0)
                        {
                            PromptDialog.Text(context, ProcessContactDetails, "Please Provide the correct scheme name");
                        }
                    }
                    else
                    {
                        PromptDialog.Text(context, ProcessContactDetails, "Please Provide the correct scheme name");
                    }
                }
                else if (PromptDialogCount == PromptDialogFor.ConfirmScheme)
                {
                    schemeSelected = UserResponse.ToString();
                    SQLQuery = "SELECT * FROM scheme WHERE LOWER(sch_description) = '" + schemeSelected + "'";
                    dt = commonObject.GetDTForQuery(SQLQuery);

                    for (int i = 0; i < ContactPerson.Count; i++)
                    {
                        returnedContactPerson = commonObject.returnContactDetails(dt, ContactPerson[i]);
                        if (returnedContactPerson != null && returnedContactPerson != "")
                        {
                            await context.PostAsync(ContactPerson[i] + " : " + returnedContactPerson);
                        }
                        else if (returnedContactPerson == null)
                        {
                            await context.PostAsync(ContactPerson[i] + " doesnt exist, or try again later");
                        }
                        else if (returnedContactPerson == "")
                        {
                            await context.PostAsync("Sorry we dont have record for " + ContactPerson[i]);
                        }
                    }
                    userIntent = IntentSelected.None;
                    PromptDialogCount = PromptDialogFor.None;
                    context.Wait(MessageReceived);
                }
            }
            catch (Exception ex)
            {
                await context.PostAsync(ex.Message);
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("None")]
        [LuisIntent("")]
        public async Task GetSchemeName(IDialogContext context, LuisResult result)
        {
            if (userIntent != IntentSelected.None)
            {
                if (userIntent == IntentSelected.LowerAccrualRates)
                {
                    await context.PostAsync("Lower Accrual Rate is 80th");
                    context.Wait(MessageReceived);
                }
                else if (userIntent == IntentSelected.MiddleAccrualRates)
                {
                    await context.PostAsync("Middle Accrual Rate is 60ths");
                    context.Wait(MessageReceived);
                }
                else if (userIntent == IntentSelected.UpperAccrualRates)
                {
                    await context.PostAsync("Upper Accrual Rate is 50ths");
                    context.Wait(MessageReceived);
                }

                else if (userIntent == IntentSelected.AccrualRates)
                {
                    AccrualType.Clear();
                    AccrualType.Add("Middle Rate");
                    AccrualType.Add("Lower Rate");
                    AccrualType.Add("Upper Rate");
                    PromptDialog.Choice(context, ProcessAccrualRate, AccrualType, "Select the Accrual Level?", "not the valid option", 3, PromptStyle.Auto);
                }

                else if (true)
                {
                    await context.PostAsync("your scheme code is " + result.Query);
                    context.Wait(MessageReceived);
                }
                else
                {
                    await context.PostAsync("Enter the correct scheme code");
                    context.Wait(MessageReceived);
                }


            }

            else
            {
                await context.PostAsync("Sorry I am not able to understand you \n\n can you please reframe your question");
                context.Wait(MessageReceived);
            }




        }


    }


}