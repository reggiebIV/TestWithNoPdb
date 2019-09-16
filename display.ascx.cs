using System;
using System.Collections.Generic;
using System.Web.UI;
using BBNCExtensions.Parts;
using BBNCExtensions.API.Transactions;
using BBNCExtensions.API.DataObjects;
using BBNCExtensions.API.Transactions.Donations;
using BBNCExtensions.Interfaces.Services;
using System.Text.RegularExpressions;
using Blackbaud.Web.Content.Core;
using Blackbaud.Web.Content.Core.Data;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using System.Web.UI.WebControls;
using System.Web;
using System.Data.SqlClient;

namespace FFD_Donation_Form
{
    public partial class Display : CustomPartDisplayBase
    {
        public Guid merchantAccountGuid;
        public int AppealId;
        public int CampaignId;
        public int FundId;
        public string savedConfirmationHtml;
        public string savedEmailHtml;
        public int driveType;
        public string emailTemplateIdString;
        public string driveTypeString;
        public Guid transactionId;
        public decimal userDonationAmount;
        DonationFormData dataToPassToDonationForm = new DonationFormData();
        public string emailTemplateName;
        public string emailHtmlRaw;
        public string subject;
        public string fromName;
        public string fromAddress;
        public string replyAddress;
        public int emailTemplateId;
        public string additionalRecipients = "";
        public string connectString = "server=SEHAR-SQL;user id=ShelbyPortal;pwd=Pr0v!d1ngF00d;database=RE";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["confirmationHtml"] != null)
            {
                confirmationWrap.InnerHtml = Session["confirmationHtml"].ToString();
                confirmationWrap.Attributes.Add("class", "");
                SHFBDonationFormWrapper.Attributes.Add("class", "noHeight");
                Session["confirmationHtml"] = null;
            }
            else
            {
                try
                {
                    donationFormSettings retrieveSavedData = Content.GetContent(typeof(donationFormSettings)) as donationFormSettings;
                    donationAmountLabel.Text = retrieveSavedData.amountLabel;
                    titleLabel.Text = retrieveSavedData.titleLabel;
                    nameFirstLabel.Text = retrieveSavedData.firstNameLabel;
                    nameLastLabel.Text = retrieveSavedData.lastNameLabel;
                    emailAddressLabel.Text = retrieveSavedData.emailAddressLabel;
                    anonymousLabel.Text = retrieveSavedData.anonymousLabel;
                    phoneNumberLabel.Text = retrieveSavedData.phoneNumberLabel;
                    streetAddresLabel.Text = retrieveSavedData.streetAddressLabel;
                    cityLabel.Text = retrieveSavedData.cityLabel;
                    stateLabel.Text = retrieveSavedData.stateLabel;
                    zipCodeLabel.Text = retrieveSavedData.zipLabel;
                    creditCardNumberLabel.Text = retrieveSavedData.creditCardNumberLabel;
                    creditCscLabel.Text = retrieveSavedData.cscLabel;
                    creditExpirationLabel.Text = retrieveSavedData.expirationLabel;
                    savedConfirmationHtml = retrieveSavedData.confirmationHtml;
                    savedEmailHtml = retrieveSavedData.acknowledgementHtml;
                    driveTypeString = retrieveSavedData.selectedRegistrationString;
                    transactionId = retrieveSavedData.customTransactionGuid;
                    donationTransId.Value = transactionId.ToString();
                    Guid RegistrationTransactionId = new Guid(retrieveSavedData.registrationTransactionGuid);

                    AppealId = retrieveSavedData.appealId;
                    CampaignId = retrieveSavedData.campaignId;
                    FundId = retrieveSavedData.fundId;
                    driveType = retrieveSavedData.selectedRegistrationType;
                    driveTypeString = retrieveSavedData.selectedRegistrationString;
                    emailTemplateId = retrieveSavedData.emailTemplateId;
                    emailTemplateName = retrieveSavedData.emailTemplateName;

                    subject = retrieveSavedData.subjectLine;
                    fromName = retrieveSavedData.fromName;
                    fromAddress = retrieveSavedData.fromAddress;
                    replyAddress = retrieveSavedData.replyAddress;
                    additionalRecipients = retrieveSavedData.additionalRecipients;

                    driveTypeId.Value = driveType.ToString();
                    sortOrder.Value = retrieveSavedData.sortOrder;
                    merchantAccountGuid = new Guid(retrieveSavedData.merchantAccountGuid);

                    emailTemplateIdString = retrieveSavedData.emailTemplateIdString;

                    //Location_ID.Value = "100";

                    getPastDonationData();
                    setYears();

                    //string fundraiserPage = Request.QueryString["fundraiser"];
                    string urlPath = HttpContext.Current.Request.Url.AbsolutePath.Replace("/", "").ToLower();
                    CustomTransaction[] allTransactions;
                    Blackbaud.Web.Content.Core.Extensions.API.Transactions.CustomTransactions transactionRetriever = new Blackbaud.Web.Content.Core.Extensions.API.Transactions.CustomTransactions();
                    int idOfTransactionForPage;
                    int currentUser = API.Users.CurrentUser.UserID;

                    try
                    {
                        allTransactions = transactionRetriever.GetTransactionsByType(RegistrationTransactionId);
                        foreach (CustomTransaction transaction in allTransactions)
                        {
                            string thisTransaction = transaction.Data;
                            string thisUrl = "";
                            string firstName = "";
                            string lastName = "";
                            string emailAddress = "";
                            string thisDollarGoal = "";
                            string thisPoundGoal = "";
                            StringReader stringReader = new StringReader(thisTransaction);
                            XmlTextReader reader = new XmlTextReader(stringReader);

                            while (reader.Read())
                            {
                                if (reader.IsStartElement())
                                {
                                    switch (reader.Name)
                                    {
                                        case "friendlyUrl":
                                            thisUrl = reader.ReadString().ToLower();
                                            if (thisUrl == urlPath)
                                            {
                                                idOfTransactionForPage = transaction.ID;
                                            }
                                            break;
                                        case "userId":
                                            dataToPassToDonationForm.solicitorId = int.Parse(reader.ReadString());
                                            break;
                                        case "teamNames":
                                            dataToPassToDonationForm.Teams = reader.ReadString();
                                            break;
                                        case "orgName":
                                            dataToPassToDonationForm.solicitorOrg = reader.ReadString();
                                            break;
                                        case "firstName":
                                            firstName = reader.ReadString();
                                            dataToPassToDonationForm.firstName = firstName;
                                            break;
                                        case "lastName":
                                            lastName = reader.ReadString();
                                            dataToPassToDonationForm.lastName = lastName;
                                            break;
                                        case "emailAddress":
                                            emailAddress = reader.ReadString();
                                            break;
                                        case "newOrgId":
                                            dataToPassToDonationForm.orgId = int.Parse(reader.ReadString());
                                            break;
                                        case "totalGoal":
                                            thisDollarGoal = reader.ReadString();
                                            break;
                                        case "poundGoal":
                                            thisPoundGoal = reader.ReadString();
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }

                            if (urlPath == thisUrl)
                            {
                                donationData.Value = JsonConvert.SerializeObject(dataToPassToDonationForm);
                                totalGoal.Value = thisDollarGoal;
                                poundGoal.Value = thisPoundGoal;
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("pagedesign"))
                        {
                            /*System.Web.UI.HtmlControls.HtmlGenericControl createDiv = new System.Web.UI.HtmlControls.HtmlGenericControl("DIV");
                            createDiv.ID = "donationFormError";
                            createDiv.InnerHtml = */
                            ExceptionWrap.InnerHtml = "Please make sure to set the registration form GUID when editing the donation form";
                        }
                    }
                }

                catch (Exception err)
                {
                    SHFBDonationFormWrapper.InnerHtml = "Please check part configuration.";
                }
                getTitles();
            }
        }
        public void submitDonationTransaction(object sender, EventArgs e)
        {
            bool dataIsVald = validateData();

            if (dataIsVald)
            {
                PaymentArgs donationTransaction = new PaymentArgs();
                Page thisPage = new Page();
                Blackbaud.Web.Content.Core.Extensions.API.Transactions.Transactions getDonationDefaults = new Blackbaud.Web.Content.Core.Extensions.API.Transactions.Transactions(thisPage);
                Address donorAddress = new Address();
                Blackbaud.Web.Content.Core.Extensions.API.Transactions.Transactions recordDonation = new Blackbaud.Web.Content.Core.Extensions.API.Transactions.Transactions(thisPage);
                RecordDonationReply donationResponse = new RecordDonationReply();
                List<PaymentArgs.DesignationInfo> designations = new List<PaymentArgs.DesignationInfo>();
                PaymentArgs.DesignationInfo myDonationDesignation = new PaymentArgs.DesignationInfo();
                string cardName = creditCardType.Value;
                CreditCardType donationFormCardType = (CreditCardType)Enum.Parse(typeof(CreditCardType), cardName);

                List<AttributeValue> giftAttributes = new List<AttributeValue>();
                AttributeValue orgDriveCoordinator = new AttributeValue();
                AttributeValue orgDriveSolicitor = new AttributeValue();
                AttributeValue driveId = new AttributeValue();
                AttributeValue teamId = new AttributeValue();
                AttributeValue lowDollarReceipt = new AttributeValue();

                //Coordinator Attribute - This was causing processing errors. So we're going to remove that from transactions
                //orgDriveCoordinator.AttributeTypeId = 211;
                //orgDriveCoordinator.Value = OrgDriveCoordinator.Value;

                //Solicitor
                orgDriveSolicitor.AttributeTypeId = 210;
                orgDriveSolicitor.Value = solicitorOrgId.Value;

                //ID of Associated Drive
                driveId.AttributeTypeId = 97;
                driveId.Value = driveTypeString;

                //Team ID
                teamId.AttributeTypeId = 102;
                teamId.Value = Location_ID.Value;

                //Low Dollar Receipt Requested
                lowDollarReceipt.AttributeTypeId = 84;
                lowDollarReceipt.Value = "No";


                //consoleLog.Value = "Attribute ID: " + orgDriveCoordinator.AttributeTypeId.ToString() + " Value: " + orgDriveCoordinator.Value + " Attribute ID: " + driveId.AttributeTypeId.ToString() + " Value: " + driveId.Value;

                //Add attributes to the list
                //giftAttributes.Add(orgDriveCoordinator);
                giftAttributes.Add(orgDriveSolicitor);
                giftAttributes.Add(driveId);
                giftAttributes.Add(teamId);
                giftAttributes.Add(lowDollarReceipt);

                myDonationDesignation.Amount = userDonationAmount;
                myDonationDesignation.BackofficeId = FundId;
                myDonationDesignation.Description = "Wherever the need is greatest";
                designations.Add(myDonationDesignation);

                donorAddress.StreetAddress = streetAddressInput.Text;
                donorAddress.City = city.Text;
                donorAddress.StateProvince = tempState.Value;
                donorAddress.ZIP = zipCode.Text;

                donationTransaction = getDonationDefaults.CreatePaymentArgs(true);
                donationTransaction.Title = title.SelectedItem.Text;
                donationTransaction.FirstName = nameFirst.Text;
                donationTransaction.LastName = nameLast.Text;
                donationTransaction.EmailAddress = emailAddress.Text;
                donationTransaction.DonorAddress = donorAddress;
                donationTransaction.IsAnonymous = anonymous.Checked;
                donationTransaction.CreditCardHolderName = nameFirst.Text + ' ' + nameLast.Text;
                donationTransaction.CreditCardNumber = creditCardNumber.Text;
                donationTransaction.CreditCardCSC = creditCsc.Text;
                donationTransaction.CreditCardExpirationMonth = int.Parse(tempMonth.Value);
                donationTransaction.CreditCardExpirationYear = int.Parse(tempYear.Value);
                donationTransaction.CreditCardTypeName = cardName;
                donationTransaction.CreditCardType = donationFormCardType;
                donationTransaction.BBPS_MerchantAccountID = merchantAccountGuid;
                donationTransaction.Designations = designations;
                donationTransaction.AppealID = AppealId;
                donationTransaction.GiftAttributes = giftAttributes;
                donationTransaction.Comments = "Org: " + orgName.Value + " " + solicitorOrgId.Value + " Drive ID: " + driveTypeId.Value + ' ' + "Coordinator: " + OrgDriveCoordinator.Value + "Location ID: " + Location_ID.Value;

                try
                {
                    donationResponse = recordDonation.RecordDonation(donationTransaction);
                    if (donationResponse.CreditCardAuthorizationResponse.GatewayResultCode == 0)
                    {
                        submitCustomTransactionForRecord(donationResponse.CreditCardAuthorizationResponse.AUTHCODE, donationResponse.NetCommunityTransactionId);
                    }
                    else
                    {
                        ExceptionWrap.InnerHtml += "<div style=\"display: none;\">" + donationResponse.CreditCardAuthorizationResponse.RAW_RESULT_STATUS + "</div>";
                        ExceptionWrap.InnerHtml += "<div>" + donationResponse.CreditCardAuthorizationResponse.REASONCODE + "</div>";
                        if (creditCsc.Text.Length != 3 && creditCsc.Text.Length != 4)
                        {
                            ExceptionWrap.InnerHtml += "<div>Please check your CSC code </div>" + creditCsc.Text.Length;
                        }

                    }
                }
                catch (Exception err)
                {
                    ExceptionWrap.InnerHtml += "There was an error with your donation. Please contact SHFB for more information. CODE: 8 And the ID: " + emailTemplateId.ToString();
                    ExceptionWrap.InnerHtml += "<div>" + err.ToString() + "</div>";
                }
            }
        }
        public void submitCustomTransactionForRecord(string authCode, int bbncTransactionId)
        {
            DateTime now = DateTime.UtcNow;
            var tz = TimeZoneInfo.Local;
            bool isDst = tz.IsDaylightSavingTime(now);
            double offset;

            if (isDst)
            {
                now = now.AddHours(-7);
            }
            else
            {
                now = now.AddHours(-8);
            }
            donationCustomTransaction thisTransaction = new donationCustomTransaction();
            thisTransaction.cardHolderName = nameFirst.Text + ' ' + nameLast.Text;
            thisTransaction.date = now.ToString();
            thisTransaction.authCode = authCode;
            thisTransaction.amount = decimal.Parse(donationAmount.Text);
            thisTransaction.bbncTransactionId = bbncTransactionId;
            thisTransaction.teamId = Location_ID.Value;
            thisTransaction.teamName = teamName.Value;
            thisTransaction.GiftType = "Cash";
            thisTransaction.driveId = int.Parse(driveTypeId.Value);

            if (Request.QueryString["fundraiser"] != null)
            {
                thisTransaction.pageName = Request.QueryString["fundraiser"];
            }
            else
            {
                thisTransaction.pageName = HttpContext.Current.Request.Url.AbsolutePath.Replace("/", "");
            }

            thisTransaction.emailAddress = emailAddress.Text;
            thisTransaction.onlineGift = true;
            thisTransaction.donorAddress = streetAddressInput.Text + ',' + city.Text + ',' + tempState.Value + ',' + zipCode.Text;
            thisTransaction.solicitorName = orgName.Value;

            if (IsVrGift.Value == "true")
            {
                thisTransaction.isVrGift = true;
            }
            else
            {
                thisTransaction.isVrGift = false;
            }
            Blackbaud.Web.Content.Core.Extensions.API.Transactions.CustomTransactions myTransaction = new Blackbaud.Web.Content.Core.Extensions.API.Transactions.CustomTransactions();

            myTransaction.RecordTransaction(transactionId, thisTransaction);
            displayConfirmation();
        }
        public void displayConfirmation()
        {
            string rawConfirmationHtml = savedConfirmationHtml;
            string CcLastFour = "";
            string justLast4 = creditCardNumber.Text.Substring(creditCardNumber.Text.Length - 4);
            DateTime Now = DateTime.Today;
            Now = Now.Date;
            string todayString = Now.ToString();

            for (int i = 0; i < creditCardNumber.Text.Length - 3; i++)
            {
                CcLastFour += '*';
            }
            CcLastFour += justLast4;

            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[first_name\]", nameFirst.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[last_name\]", nameLast.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[email_address\]", emailAddress.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[street_address\]", streetAddressInput.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[city\]", city.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[state\]", state.SelectedItem.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[zip_code\]", zipCode.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[phone_number\]", phoneNumber.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[gift_amount\]", donationAmount.Text);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[Gift_Date\]", todayString);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[payment_method\]", creditCardType.Value);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[Last_4_CC\]", CcLastFour);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[gift_solicitor\]", orgName.Value);
            rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[fundraiser_page_url\]", "<a href=" + Request.Url.ToString() + ">" + Request.Url.ToString() + "</a>");

            if (teamName.Value == "")
            {
                rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"(?=\[if_team_name\]).*?(?<=\[end_if\])", "");
            }
            else
            {
                rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[team_name\]", teamName.Value);
                rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[if_team_name\]", "");
                rawConfirmationHtml = Regex.Replace(rawConfirmationHtml, @"\[end_if\]", "");
            }


            //confirmationWrap.InnerHtml = rawConfirmationHtml;
            //confirmationWrap.Attributes.Add("class", "");
            //SHFBDonationFormWrapper.Attributes.Add("class", "noHeight");
            Session["confirmationHtml"] = rawConfirmationHtml;
            sendConfirmationEmail();
        }
        public void sendConfirmationEmail()
        {
            string emailHtml = savedEmailHtml;
            int emailTemplateId = int.Parse(emailTemplateIdString);
            IDataProvider[] someDataProviders = new IDataProvider[2];
            int bbncUserId = API.Users.CurrentUser.UserID;
            int reId = 1;
            string CcLastFour = "";
            string justLast4 = creditCardNumber.Text.Substring(creditCardNumber.Text.Length - 4);

            for (int i = 0; i < creditCardNumber.Text.Length - 3; i++)
            {
                CcLastFour += '*';
            }
            CcLastFour += justLast4;

            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[first_name\]", nameFirst.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[last_name\]", nameLast.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[email_address\]", emailAddress.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[street_address\]", streetAddressInput.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[city\]", city.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[state\]", state.SelectedItem.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[zip_code\]", zipCode.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[phone_number\]", phoneNumber.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[gift_amount\]", donationAmount.Text);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[Gift_Date\]", DateTime.Today.Date.ToString());
            savedEmailHtml = Regex.Replace(savedEmailHtml, @" 12:00:00 AM", "");
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[payment_method\]", creditCardType.Value);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[Last_4_CC\]", CcLastFour);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[gift_solicitor\]", orgName.Value);
            savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[fundraiser_page_url\]", "<a href=" + Request.Url.ToString() + ">" + Request.Url.ToString() + "</a>");

            if (teamName.Value == "")
            {
                savedEmailHtml = Regex.Replace(savedEmailHtml, @"(?=\[if_team_name\]).*?(?<=\[end_if\])", "");
            }
            else
            {
                savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[team_name\]", teamName.Value);
                savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[if_team_name\]", "");
                savedEmailHtml = Regex.Replace(savedEmailHtml, @"\[end_if\]", "");
            }


            EmailTemplate emailTemplate = new EmailTemplate();
            emailTemplate.ClientSitesID = PortalSettings.Current().ClientSitesID;
            emailTemplate.ContentText = string.Empty;
            emailTemplate.Description = string.Empty;
            emailTemplate.ID = 224;
            emailTemplate.Name = emailTemplateName;
            emailTemplate.Subject = "Subject";
            emailTemplate.OwnerID = 0;
            emailTemplate.AuditInfo = null;

            EMail myEmail = new EMail(emailTemplate);

            myEmail.Name = "Fund Drive Donation Confirmation";
            myEmail.FromAddress = fromAddress;
            myEmail.FromDisplayName = fromName;
            myEmail.Subject = subject;
            myEmail.ContentHTML = savedEmailHtml + "<p><a href=\"target=&amp;pid=187&amp;did=0&amp;tab=0\">link</a> | <a href=\"target=&amp;pid=188&amp;did=0&amp;tab=0\"> link </a></p>";
            ExceptionWrap.InnerHtml += "<div>Template ID: " + emailTemplate.ID.ToString() + "</div>";
            myEmail.Save();
            myEmail.Send(emailAddress.Text, fromName, reId, bbncUserId, someDataProviders, this.Page);

            if (additionalRecipients != null)
            {
                string[] recipientArray = additionalRecipients.Split(',');

                foreach (string thisRecipient in recipientArray)
                {
                    myEmail.Send(thisRecipient, fromName, reId, bbncUserId, someDataProviders, this.Page);
                }
            }

            Response.Redirect(Request.Url.AbsoluteUri);
        }
        public void getPastDonationData()
        {
            CustomTransaction[] allDonationTransactions;
            Blackbaud.Web.Content.Core.Extensions.API.Transactions.CustomTransactions donationTransactionRetriever = new Blackbaud.Web.Content.Core.Extensions.API.Transactions.CustomTransactions();
            allDonationTransactions = donationTransactionRetriever.GetTransactionsByType(transactionId);
            List<donationCustomTransaction> allDonationData = new List<donationCustomTransaction>();
            List<donationDataForFundraiserPage> donationDataForPage = new List<donationDataForFundraiserPage>();
            string urlPath = HttpContext.Current.Request.Url.AbsolutePath.Replace("/", "").ToLower();

            foreach (CustomTransaction transaction in allDonationTransactions)
            {
                string thisTransaction = transaction.Data;
                StringReader stringReader = new StringReader(thisTransaction);
                XmlTextReader reader = new XmlTextReader(stringReader);

                string thisCardholder = "";
                string thisDate = "";
                string thisAuthCode = "";
                string thisPageName = "";
                string thisTeamId = "";
                string thisTeamName = "";
                string thisGiftType = "";
                decimal thisAmount = 0;
                int thisBbncTransactionId = 0;
                bool thisDonationDeleted = false;
                int thisDriveId = 0;

                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "cardHolderName":
                                thisCardholder = reader.ReadString();
                                break;
                            case "date":
                                thisDate = reader.ReadString();
                                break;
                            case "authCode":
                                thisAuthCode = reader.ReadString();
                                break;
                            case "teamId":
                                thisTeamId = reader.ReadString();
                                break;
                            case "teamName":
                                thisTeamName = reader.ReadString();
                                break;
                            case "amount":
                                thisAmount = decimal.Parse(reader.ReadString());
                                break;
                            case "bbncTransactionId":
                                thisBbncTransactionId = int.Parse(reader.ReadString());
                                break;
                            case "pageName":
                                thisPageName = reader.ReadString().ToLower();
                                break;
                            case "GiftType":
                                thisGiftType = reader.ReadString();
                                break;
                            case "isDeleted":
                                thisDonationDeleted = reader.ReadElementContentAsBoolean();
                                break;
                            case "driveId":
                                thisDriveId = int.Parse(reader.ReadString());
                                break;
                        }
                    }
                }

                allDonationData.Add(new donationCustomTransaction()
                {
                    cardHolderName = thisCardholder,
                    date = thisDate,
                    authCode = thisAuthCode,
                    pageName = thisPageName,
                    teamId = thisTeamId,
                    teamName = thisTeamName,
                    amount = thisAmount,
                    bbncTransactionId = thisBbncTransactionId,
                    GiftType = thisGiftType,
                    isDeleted = thisDonationDeleted,
                    driveId = thisDriveId
                });
            }

            foreach (donationCustomTransaction thisTransaction in allDonationData)
            {
                if (thisTransaction.pageName == urlPath)
                {
                    if (!thisTransaction.isDeleted && thisTransaction.driveId == driveType)
                    {
                        donationDataForPage.Add(new donationDataForFundraiserPage()
                        {
                            teamId = thisTransaction.teamId,
                            teamName = thisTransaction.teamName,
                            amount = thisTransaction.amount,
                            GiftType = thisTransaction.GiftType,
                            pageName = thisTransaction.pageName,
                            driveId = thisTransaction.driveId
                        });
                    }
                }
            }

            summaryDonationData.Value = JsonConvert.SerializeObject(donationDataForPage);
        }
        public bool validateData()
        {
            bool dataIsValid = true;
            string validationMessage = "";

            try
            {
                userDonationAmount = Decimal.Parse(donationAmount.Text);
            }
            catch (Exception err)
            {
                dataIsValid = false;
                validationMessage += "<div>Please input numbers only into the Gift Amount field</div>";
            }

            int n;
            bool isNumeric = int.TryParse(zipCode.Text, out n);
            if (!isNumeric || zipCode.Text.Length != 5)
            {
                dataIsValid = false;
                validationMessage += "<div>Please enter 5 numeric characters in the zip field</div>";
            }

            bool isEmail = IsValidEmail(emailAddress.Text);
            if (!isEmail)
            {
                dataIsValid = false;
                validationMessage += "<div>Please enter a valid email address</div>";
            }

            if (streetAddressInput.Text == "")
            {
                dataIsValid = false;
                validationMessage += "<div>Please ensure that you enter a billing address for your donation</div>";
            }

            if (nameFirst.Text == "" || nameLast.Text == "")
            {
                dataIsValid = false;
                validationMessage += "<div>Please enter a first and last name</div>";
            }

            if (creditCsc.Text.Length > 4)
            {
                dataIsValid = false;
                validationMessage += "<div>Please check your CSC code</div>";
            }

            if (creditCardNumber.Text.Length < 12)
            {
                dataIsValid = false;
                validationMessage += "<div>Please enter a valid credit card number</div>";
            }

            ExceptionWrap.InnerHtml = validationMessage;
            return dataIsValid;
        }
        public bool IsValidEmail(string emailAddress)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(emailAddress);
                return addr.Address == emailAddress;
            }
            catch
            {
                return false;
            }
        }
        public void setYears()
        {
            expirationYear.Items.Clear();
            String sDate = DateTime.Now.ToString();
            DateTime datevalue = (Convert.ToDateTime(sDate.ToString()));

            for (int i = 0; i <= 15; i++)
            {
                String yy = (datevalue.Year + i).ToString();
                expirationYear.Items.Add(new ListItem((yy).ToString(), (yy).ToString()));
            }
        }
        public void getTitles()
        {
            if (title.Items.Count == 0)
            {
                string getTitlesQuery = "Select TABLEENTRIESID, LONGDESCRIPTION from dbo.TABLEENTRIES WHERE CODETABLESID = 5013 AND ACTIVE = -1";
                string thisName = "";
                int thisId = 0;
                List<tableEntry> titleList = new List<tableEntry>();

                using (SqlConnection connection = new SqlConnection(connectString))
                {
                    connection.Open();
                    SqlCommand GrabTitlesFromDb = new SqlCommand(getTitlesQuery, connection);
                    SqlDataReader reader = GrabTitlesFromDb.ExecuteReader();
                    try
                    {
                        while (reader.Read())
                        {
                            thisId = reader.GetInt32(0);
                            thisName = reader.GetString(1);
                            titleList.Add(new tableEntry()
                            {
                                name = thisName,
                                id = thisId
                            });
                        }

                    }
                    catch (Exception err)
                    {
                        //Nothing
                    }
                }


                foreach (tableEntry thisTitle in titleList)
                {
                    try
                    {
                        title.Items.Add(new ListItem(thisTitle.name, thisTitle.id.ToString()));
                    }
                    catch (Exception err)
                    {
                        //Nothing
                    }
                }
            }
        }
        public class tableEntry
        {
            public string name { get; set; }
            public int id { get; set; }
        }
    }
    public class donationDataForFundraiserPage
    {
        public string teamId { get; set; }
        public string teamName { get; set; }
        public decimal amount { get; set; }
        public string GiftType { get; set; }
        public string pageName { get; set; }
        public int driveId { get; set; }
    }
}