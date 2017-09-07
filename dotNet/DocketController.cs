using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.UI.WebControls.WebParts;
using LS.Admin.Data;
using LS.Admin.Data.EF;
using LS.Admin.Data.Models;
using LS.Admin.Data.Models.Enum;
using AccountActivity = LS.Admin.Data.Models.AccountActivity;
using Activity = LS.Admin.Data.Models.Activity;
using DocketActionItem = LS.Admin.Data.Models.DocketActionItem;
using ListingCampaignUrl = LS.Admin.Data.Models.ListingCampaignUrl;
using SocialMediaAccount = LS.Admin.Data.Models.SocialMediaAccount;

namespace LS.Web.api
{
    [Authorize]
    public class DocketController : ControllerBase
    {
        LbRepository _repository = new LbRepository();

        [HttpGet]
        [Route("api/docket/getActivities")]
        public IHttpActionResult GetActivities()
        {
            UserInfoModel currentuser = GetUserInfo();
            return Ok(_repository.ActivityViews.Where(x => x.SuccessManagerId == currentuser.SuccessManagerId).OrderByDescending(x => x.DateCreated));
            //return Ok(_repository.ActivityViews.OrderByDescending(x => x.DateCreated));
        }

        [HttpGet]
        [Route("api/docket/GetActivityAndAgents/{activityId}")]
        public IHttpActionResult GetActivityAndAgents(int activityId)
        {
            Activity activity = _repository.ActivityAndAgents.FirstOrDefault(x => x.Id == activityId);
            if (activity != null)
            {
                //set agents that are already in the AccountActivity table to selected
                activity.Agents.ForEach(x => x.Selected = true);
                //this activity has selected agents that were published too.
                //for the UI we need to show the entire list of agents for the SM
                List<int> accountsToExclude = activity.Agents.Select(x => x.AccountId).ToList();
                List<DocketAgent> agents = _repository.DocketAgents.Where(x => x.SuccessManagerId == activity.SuccessManagerId && !accountsToExclude.Contains(x.AccountId)).ToList();
                activity.Agents.AddRange(agents);
                activity.Agents = activity.Agents.OrderBy(x => x.Lastname).ToList();
                return Ok(activity);
            }
            return BadRequest("Activity not found");
        }

        /// <summary>
        /// Returns the list of campaign urls by the listing id specified.
        /// </summary>
        /// <param name="listingId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/docket/GetCampaignUrlsByListingId/{listingId}")]
        public IHttpActionResult GetCampaignUrlsByListingId(int listingId)
        {
            return Ok(_repository.ListingCampaignUrls.Where(x => x.ListingId == listingId));
        }

        [HttpGet]
        [Route("api/docket/GetSocialMediaAccountBySocialMediaSiteId/{socialMediaSiteId}")]
        public IHttpActionResult GetSocialMediaAccountBySocialMediaSiteId(int socialMediaSiteId)
        {
            return Ok(_repository.SocialMediaAccounts.Where(x => x.SocialMediaSiteId == socialMediaSiteId));
        }

        [HttpGet]
        [Route("api/docket/GetDocketAgents")]
        public IHttpActionResult GetDocketAgents()
        {
            UserInfoModel currentuser = GetUserInfo();
            //gets the list of agents to show in the docket activity page for a SM
            return Ok(_repository.DocketAgents.Where(x => x.SuccessManagerId == currentuser.SuccessManagerId));
        }

        [HttpGet]
        [Route("api/docket/GetAgentsForCampaignReady")]
        public IHttpActionResult GetAgentsForCampaignReady()
        {
            //gets the list of agents to show in the docket activity page for a SM
            return Ok(_repository.AccountsNotCampaignReadys);
        }
        [HttpGet]
        [Route("api/docket/GetAgentsForListingReady")]
        public IHttpActionResult GetAgentsForListingReady()
        {
            //gets the list of agents to show in the docket activity page for a SM
            return Ok(_repository.AccountsNotListingReadys);
        }
        [HttpGet]
        [Route("api/docket/GetListings")]
        public IHttpActionResult GetListings()
        {
            //gets the list of listings to show in the docket campaign recorder for a SM
            //return Ok(_repository.ListingDownloads.Where(x => !string.IsNullOrEmpty(x.Streetnumber) & !string.IsNullOrEmpty(x.Streetname) & !string.IsNullOrEmpty(x.StreetDesignator)));
            return Ok(_repository.ListingDownloads);
        }

        [HttpGet]
        [Route("api/docket/GetDocketActionItems/{complete}")]
        public IHttpActionResult GetDocketActionItems(bool? complete)
        {
            try
            {
                UserInfoModel currentuser = GetUserInfo();
                int? successManagerId = currentuser.SuccessManagerId;

                //this is supplying the actions grid on the docket main page 
                if (!complete.HasValue && !successManagerId.HasValue)
                {
                    //return everything
                    return Ok(_repository.DocketActionItems);
                }
                if (complete.HasValue && !successManagerId.HasValue)
                {
                    //return everything
                    return Ok(_repository.DocketActionItems.Where(x => x.IsComplete == complete));
                }
                if (complete.HasValue && successManagerId.HasValue)
                {
                    //return everything
                    return Ok(_repository.DocketActionItems.Where(x => x.IsComplete == complete && x.SuccessManagerId == successManagerId));
                }
                if (!complete.HasValue && successManagerId.HasValue)
                {
                    //return everything
                    return Ok(_repository.DocketActionItems.Where(x => x.SuccessManagerId == successManagerId));
                }

                return Ok(_repository.AccountsNotListingReadys);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("api/docket/GetAgentActivity/{activityId}")]
        public IHttpActionResult GetAgentActivity(int activityId)
        {
            //this is the call that gets the activity so show the agent so they can respond to it
            Activity activityResult = _repository.Activities.FirstOrDefault(x => x.Id == activityId);
            AccountActivity accountActivityResult = _repository.AccountActivities.FirstOrDefault(x => x.ActivtityId == activityId);

            return Ok(new { activity = activityResult, accountActivity = accountActivityResult });
        }


        [HttpPost]
        [Route("api/docket/saveActivity")]
        public IHttpActionResult SaveActivity([FromBody] Activity activity)
        {
            if (activity.SuccessManagerId == 0)
            {
                UserInfoModel currentuser = GetUserInfo();
                activity.SuccessManagerId = currentuser.SuccessManagerId;
            }
            //get the list of agents already 
            List<DocketAgent> existingVals = _repository.DocketAgents.Where(x => x.SuccessManagerId == activity.SuccessManagerId).ToList();
            try
            {
                DataWriter dw = new DataWriter();
                int actID = dw.SaveDocketActivity(activity);
                activity.Id = actID;
                if (actID > 0)
                {
                    return Ok(activity);
                }
                return StatusCode(HttpStatusCode.BadRequest);

            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    return InternalServerError(e.InnerException);
                }
                return InternalServerError(e);
            }
        }


        [HttpPost]
        [Route("api/docket/PublishActivity")]
        public IHttpActionResult PublishActivity([FromBody] Activity activity)
        {
            //List<DocketAgent> existingVals = _repository.DocketAgents.Where(x => x.SuccessManagerId == activity.SuccessManagerId).ToList();
            try
            {
                Activity existingActivity = _repository.ActivityAndAgents.FirstOrDefault(x => x.Id == activity.Id);
                if (existingActivity != null)
                {
                    List<DocketAgent> existingVals = existingActivity.Agents.ToList();

                    DataWriter dw = new DataWriter();
                    foreach (DocketAgent agent in activity.Agents)
                    {
                        if (existingVals.Any(x => x.AccountId == agent.AccountId))
                        {
                            if (agent.Selected == existingVals.First(x => x.AccountId == agent.AccountId).Selected)
                            {
                                continue;
                            }
                        }
                        if (agent.Selected)
                        {
                            dw.AddAccountActivity(agent.AccountId, activity.Id);
                        }
                        else
                        {
                            dw.DeleteAccountActivity(agent.AccountId, activity.Id);
                        }
                    }
                    return Ok();
                }
                return StatusCode(HttpStatusCode.BadRequest);

            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    return InternalServerError(e.InnerException);
                }
                return InternalServerError(e);
            }
        }

        [HttpPost]
        [Route("api/docket/AddDocketActionItem")]
        public IHttpActionResult AddDocketActionItem([FromBody]  DocketActionItem actionItem)
        {
            try
            {
                DataWriter dw = new DataWriter();
                int result = dw.AddDocketActionItem(actionItem);
                if (result > 0)
                {
                    if (actionItem.DocketActionId == (int)ActionItemType.CampaignReady)
                    {
                        dw.AccountSetCampaignReady(actionItem.AccountId);
                    }
                    if (actionItem.DocketActionId == (int)ActionItemType.ListingReady)
                    {
                        dw.AccountSetListingReady(actionItem.ListingId);
                    }
                    return Ok(new { id = result });
                }
                else
                {
                    return StatusCode(HttpStatusCode.BadRequest);
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    return InternalServerError(e.InnerException);
                }
                return InternalServerError(e);
            }
        }

        [HttpPost]
        [Route("api/docket/SetDocketActionItemComplete")]
        public IHttpActionResult SetDocketActionItemComplete([FromBody] int actionItemId)
        {
            try
            {
                DataWriter dw = new DataWriter();
                bool result = dw.DocketActionItemSetComplete(actionItemId);
                if (result)
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(HttpStatusCode.BadRequest);
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    return InternalServerError(e.InnerException);
                }
                return InternalServerError(e);
            }
        }


        [HttpGet]
        [AllowAnonymous]
        [Route("api/docket/DownloadListingInfo/{listingId}")]
        public HttpResponseMessage DownloadListingInfo(int listingId)
        {
            const string tabs = "\t";
            StringBuilder message = new StringBuilder("Listing Information - " + DateTime.Now.ToShortDateString());
            message.AppendFormat("{0}", Environment.NewLine);

            //this is the remaining input on the rooms
            ListingDownloadModel listing = _repository.ListingDownloads.FirstOrDefault(x => x.Id == listingId);

            if (listing != null)
            {
                List<ListingStepsDownloadModel> info = new List<ListingStepsDownloadModel>();
                info.Add(new ListingStepsDownloadModel() { StepId = (int)StepType.Bedrooms, Stepname = "Bedrooms", StepItemname = listing.Bedrooms.ToString() });
                info.Add(new ListingStepsDownloadModel() { StepId = (int)StepType.Bathrooms, Stepname = "Bathrooms Full", StepItemname = listing.BathsFull.ToString() });
                info.Add(new ListingStepsDownloadModel() { StepId = (int)StepType.Bathrooms, Stepname = "Bathrooms 3/4", StepItemname = listing.BathsThreeQuarter.ToString() });
                info.Add(new ListingStepsDownloadModel() { StepId = (int)StepType.Bathrooms, Stepname = "Bathrooms 1/2", StepItemname = listing.BathsHalf.ToString() });

                //these are the steps and stepitem checkboxes
                info.AddRange(_repository.ListingStepsDownloads.Where(x => x.ListingId == listingId).ToList());

                //these are the steps with "Other" inputs 
                info.AddRange(_repository.ListingStepsOtherDownloads.Where(x => x.ListingId == listingId).ToList());


                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Agent", listing.AgentName);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "MlsNumber", listing.MlsNumber);
                message.AppendFormat("{0}{1}: {2:M/d/yyyy}", Environment.NewLine, "List date", listing.Listdate);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Street number", listing.Streetnumber);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Street name", listing.Streetname);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Street Designator", listing.StreetDesignator);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Listing Area", listing.ListingArea);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Price", listing.Price);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Square Feet", listing.SquareFeet);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "YearBuilt", listing.YearBuilt);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Floors", listing.Floors);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Zoning", listing.Zoning);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Tractname", listing.Tractname);
                message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "County", listing.County);
                message.AppendFormat("{0}{1}: {2:M/d/yyyy}", Environment.NewLine, "Date Created", listing.DateCreated);

                info = info.OrderBy(x => x.StepId).ToList();

                foreach (ListingStepsDownloadModel item in info)
                {
                    message.AppendFormat("{0}{1}: {2}{3}", Environment.NewLine, item.Stepname, item.StepItemname, item.Text);
                }



                ListingImageModel listingImage = _repository.ListingImages.FirstOrDefault(x => x.ListingId == listingId);
                if (listingImage != null)
                {
                    message.AppendFormat("{0}{0}{1}", Environment.NewLine, "Feature Image");
                    message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "URL", ConvertPathToUrl(listingImage.ImagePath, listingId));
                    message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Thumbnail", ConvertPathToThumbUrl(listingImage.ImagePath, listingId));
                }

                message.AppendFormat("{0}{0}{1}{0}", Environment.NewLine, "Posts");
                List<ListingPostDownloadModel> postsDownloadModels = _repository.ListingPostDownloads.Where(x => x.ListingId == listingId).ToList();
                foreach (ListingPostDownloadModel post in postsDownloadModels)
                {
                    message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Title", post.Title);
                    message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Post", post.Text);
                    message.AppendFormat("{0}{1}: {2}{0}", Environment.NewLine, "URL", ConvertPathToUrl(post.ImagePath, listingId));
                }

                List<ListingImageModel> listingImageVideoList = _repository.ListingImages.Where(x => x.ListingId == listingId && (x.IncludeInVideo.HasValue && x.IncludeInVideo == true)).ToList();
                message.AppendFormat("{0}{0}{1}", Environment.NewLine, "Video Images");
                foreach (ListingImageModel img in listingImageVideoList)
                {
                    message.AppendFormat("{0}{0}{1}", Environment.NewLine, img.Title);
                    message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "URL", ConvertPathToUrl(img.ImagePath, listingId));
                    message.AppendFormat("{0}{1}: {2}", Environment.NewLine, "Thumbnail", ConvertPathToThumbUrl(img.ImagePath, listingId));
                }


                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, Encoding.Unicode))
                    {
                        sw.Write(message.ToString());
                    }
                    byte[] output = new byte[] { };
                    output = ms.ToArray();
                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(output) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = listing.AgentName + " Listing.txt"
                    };
                    return result;
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Format("Lising Id {0} not found.", listingId)) };
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/docket/DownloadCampaignInfo/{accountId}")]
        public HttpResponseMessage DownloadCampaignInfo(int accountId)
        {
            const string tabs = "\t";
            viewCampaignDownloadModel info = _repository.vwCampaignDownloads.FirstOrDefault(x => x.AccountId == accountId);


            if (info != null)
            {
                StringBuilder message = new StringBuilder("Campaign Information - " + DateTime.Now.ToShortDateString());
                message.AppendFormat("{0}", Environment.NewLine);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Agent", tabs, info.Agent);
                if (!string.IsNullOrWhiteSpace(info.MLSID))
                    message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "MLS Number", tabs, info.MLSID);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Success Manager", tabs, info.SuccessManager);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Product", tabs, info.Product);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Number Of Listings", tabs, info.NumberOfListings);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Unlaunched Listings", tabs, info.UnlaunchedListings);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Website", tabs, info.Website);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Brokerage", tabs, info.Brokerage);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Slogan", tabs, info.Slogan);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Agent Photo URL", tabs, info.AgentPhotoPath);
                string bannerUrl = info.BannerPhotoPath;
                if (info.BrokerageBannerId.HasValue)
                {
                    BrokerageBannerModel banner = _repository.BrokerageBanners.FirstOrDefault(x => x.Id == info.BrokerageBannerId);
                    if (banner != null)
                    {
                        bannerUrl =
                            Url.Content(ConfigurationManager.AppSettings["BrokerageBannersDirectory"] + banner.Filename);
                    }
                }
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Brokerage Banner URL", tabs, bannerUrl);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Agent ID", tabs, info.AgentId);
                message.AppendFormat("{0}{1}:{2}{3:M/d/yyyy}", Environment.NewLine, "Payments Start Date", tabs, info.PaymentsStartDate);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "AuthNet SubscriberId", tabs, info.AuthNetSubscriberId);
                message.AppendFormat("{0}{1}:{2}{3:M/d/yyyy}", Environment.NewLine, "Date Charged", tabs, info.DateCharged);
                message.AppendFormat("{0}{1}:{2}{3:M/d/yyyy}", Environment.NewLine, "Date Created", tabs, info.DateCreated);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, Encoding.Unicode))
                    {
                        sw.Write(message.ToString());
                    }
                    byte[] output = new byte[] { };
                    output = ms.ToArray();
                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(output) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = info.Agent + " Campaign.txt"
                    };
                    return result;
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Format("Account Id {0} not found.", accountId)) };
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/docket/DownloadOpenHouseInfo/{listingId}")]
        public HttpResponseMessage DownloadOpenHouseInfo(int listingId)
        {
            const string tabs = "\t";
            //there can be mulitple open houses on a lising to get the mose recent one.
            ListingOpenHouseModel info = _repository.ListingOpenHouses.Where(x => x.ListingId == listingId).OrderByDescending(x=>x.Id).FirstOrDefault();

            

            if (info != null)
            {
                StringBuilder message = new StringBuilder("Open House Information - " + DateTime.Now.ToShortDateString());
                message.AppendFormat("{0}", Environment.NewLine);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Agent", tabs, info.Agent);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Address", tabs, info.Address);
                message.AppendFormat("{0}{1}:{2}{3:M/d/yyyy}", Environment.NewLine, "Open House Date", tabs, info.OpenHouseDate);
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Start Time", tabs, FormatTime(info.StartTime));
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "End Time", tabs, FormatTime(info.EndTime));
                message.AppendFormat("{0}{1}:{2}{3}", Environment.NewLine, "Other Information", tabs, info.Information);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms, Encoding.Unicode))
                    {
                        sw.Write(message.ToString());
                    }
                    byte[] output = new byte[] { };
                    output = ms.ToArray();
                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(output) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = info.Agent + " OpenHouse.txt"
                    };
                    return result;
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Format("Listing Id {0} not found.", listingId)) };
            }
        }

        private string FormatTime(string timevalue)
        {
            //the values from the time control return the time values in 24 hour.
            //here we are going to format them to AM/PM
            if (string.IsNullOrWhiteSpace(timevalue)) return timevalue;
            if (!timevalue.Contains(":")) return timevalue;
            char[] delim = new char[]{':'};
            string[] parts = timevalue.Split(delim, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                int hours = int.Parse(parts[0]);
                string AmPm = "AM";
                if (hours > 12)
                {
                    hours -= 12;
                    AmPm = "PM";
                }
                else if (hours == 12)
                {
                    AmPm = "PM";
                }
                return string.Format("{0}:{1} {2}", hours, parts[1], AmPm);
            }
            return timevalue;
        }

        [HttpPost]
        [Route("api/docket/saveCampaignRecorderListings")]
        public IHttpActionResult SaveCampaignRecorderListings([FromBody] List<ListingCampaignUrl> listingCampaignUrls)
        {
            try
            {
                bool saveSuccess = true;
                bool addNewRecord = true;

                foreach (ListingCampaignUrl listing in listingCampaignUrls)
                {
                    DataWriter dw = new DataWriter();

                    SocialMediaAccount sma = new SocialMediaAccount()
                    {
                        Password = listing.Password,
                        SocialMediaSiteId = listing.SocialMediaSiteId,
                        AccountId = listing.AccountId,
                        Url = listing.Url,
                        Username = listing.UserName,
                        Id = listing.SocialMediaAccountId
                    };

                    try
                    {
                        // Update first. If our SocialMediaAccountId is a number greater than zero, that means we have an existing record which we need to update.
                        if (listing.SocialMediaAccountId > 0)
                        {
                            addNewRecord = false;
                            List<SocialMediaAccount> singleMediaAccount = new List<SocialMediaAccount>();
                            singleMediaAccount.Add(sma);
                            dw.SaveListingCampaignUrl(listing);
                            dw.UpdateSocialMediaAccounts(singleMediaAccount);
                        }
                    }
                    catch (Exception)
                    {
                        saveSuccess = false;
                    }
                    
                    // If we did not 'return' prior to here, it means we default this to an 'add'.
                    // First we insert a record into the SocialMediaAccount table and get its id which is needed for the ListingCampaignUrl table.
                    if (addNewRecord)
                    {
                        listing.SocialMediaAccountId = dw.AddSocialMediaAccount(sma);

                        if (listing.SocialMediaAccountId > 0)
                        {
                            int newId = dw.SaveListingCampaignUrl(listing);
                            listing.ListingCampaignUrlId = newId;
                            if (newId < 1)
                                saveSuccess = false;
                        }
                    }
                }

                if (saveSuccess)
                    return Ok(listingCampaignUrls);

                return StatusCode(HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    return InternalServerError(e.InnerException);
                }
                return InternalServerError(e);
            }
        }

        [HttpPost]
        [Route("api/docket/saveCampaignRecorderForAgents")]
        public IHttpActionResult SaveCampaignRecorderForAgents([FromBody] List<SocialMediaAccount> urls)
        {
            try
            {
                DataWriter dw = new DataWriter();
                bool saveSuccess = dw.AddSocialMediaAccounts(urls);
                if (saveSuccess)
                    return Ok(urls);
                return StatusCode(HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    return InternalServerError(e.InnerException);
                }
                return InternalServerError(e);
            }
        }
    }
}
