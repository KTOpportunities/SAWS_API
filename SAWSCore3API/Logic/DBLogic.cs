using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Web;

using SAWSCore3API.Authentication;
using SAWSCore3API.DBModels;
using SAWSCore3API.Filters;

namespace SAWSCore3API.Logic
{
    public class DBLogic
    {
        public ApplicationDbContext _context;

        public IConfiguration _configuration { get; }

        public DBLogic( ApplicationDbContext applicationDbContext)
        {
            _context = applicationDbContext;
        }

        public DBLogic( ApplicationDbContext applicationDbContext, IConfiguration configuration)
        {
            _context = applicationDbContext;
            _configuration = configuration;
        }

        public void InsertUpdateUserProfile(UserProfile user)
        {
            user.updated_at = DateTime.Now;
            user.isdeleted = false;
            bool insertMode = user.userprofileid == 0;
            try
            {
                if (user != null)
                {
                    if (insertMode)
                    {
                        //check if user status is set
                        //if (user.userstatusid >0 )
                        //{
                        //    user.userstatusid_moddatetime = DateTime.Now;
                        //}
                        user.created_at = DateTime.Now;
                       
                        _context.userProfiles.Add(user);
                    }
                    else
                    {
                        var local = _context.Set<UserProfile>()
                    .Local
                    .FirstOrDefault(f => f.userprofileid == user.userprofileid);

                      
                        if (local != null)
                        {
                            _context.Entry(local).State = EntityState.Detached;
                        }
                        _context.Entry(user).State = EntityState.Modified;
                    }

                    _context.SaveChanges();
                }



            }
            catch (Exception err)
            {
                string errMessage = err.Message;
                // Write to log

                //throw;
            }
        }

        public void DeleteUserProfileById(int id)
        {

            bool insertMode = id == 0;

            var record = _context.userProfiles.Where(d => d.userprofileid == id).FirstOrDefault();

            try
            {
                if (record != null)
                {
                    if (insertMode)
                    {
                        record.created_at = DateTime.Now;
                        //_context.manualrequests.Add(manrequest);
                    }
                    else
                    {
                        record.isdeleted = true;
                        record.deleted_at = DateTime.Now;
                        var local = _context.Set<UserProfile>()
                    .Local
                    .FirstOrDefault(f => f.userprofileid == id);
                        if (local != null)
                        {
                            _context.Entry(local).State = EntityState.Detached;
                        }
                        _context.Entry(record).State = EntityState.Modified;
                    }

                    _context.SaveChanges();
                }
            }
            catch (Exception err)
            {
                string errMessage = err.Message;
                // Write to log

                throw;
            }
        }

        public string PostInsertNewFeeback(Feedback feedback)
        {
            var message = "";

            ProcessFeedbackMessage(feedback);
            
            if (feedback.feedbackId == 0)
            {
                try
                {
                    feedback.created_at = DateTime.Now;
                    feedback.updated_at = DateTime.Now;
                    feedback.isdeleted = false;

                    _context.Feedbacks.Add(feedback);
                    _context.SaveChanges();
                    message = "Success";
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                feedback.updated_at = DateTime.Now;
                feedback.isdeleted = false;

                _context.Feedbacks.Update(feedback);
                _context.SaveChanges();
                message = "Success";
            }

            return message;
        }

        public string PostInsertBroadcastMessages(Feedback feedback, string batchId, string broadcastId)
        {
            var message = "";

            ProcessBroadcastMessage(feedback, broadcastId);
            
            if (feedback.feedbackId == 0)
            {
                try
                {
                    feedback.created_at = DateTime.Now;
                    feedback.updated_at = DateTime.Now;
                    feedback.isdeleted = false;
                    feedback.batchId = batchId;

                    _context.Feedbacks.Add(feedback);
                    _context.SaveChanges();
                    message = "Success";
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                feedback.updated_at = DateTime.Now;
                feedback.isdeleted = false;

                _context.Feedbacks.Update(feedback);
                _context.SaveChanges();
                message = "Success";
            }

            return message;
        }

        public void ProcessFeedbackMessage(Feedback feedback) 
        {
            foreach (var feedbackMessage in feedback.FeedbackMessages)
            {
                feedbackMessage.responderId = feedback.responderId;
                feedbackMessage.responderEmail = feedback.responderEmail;
                feedbackMessage.created_at = DateTime.Now;
                feedbackMessage.updated_at = DateTime.Now;
                feedbackMessage.isdeleted = false;
           }
        }

           public void ProcessBroadcastMessage(Feedback feedback, string broadcastId)
        {
            foreach (var feedbackMessage in feedback.FeedbackMessages)
            {
                feedbackMessage.responderId = feedback.responderId;
                feedbackMessage.responderEmail = feedback.responderEmail;
                feedbackMessage.created_at = DateTime.Now;
                feedbackMessage.updated_at = DateTime.Now;
                feedbackMessage.isdeleted = false;
                feedbackMessage.broadcastId = broadcastId;
           }
        }

        public string DeleteFeedback(int id)
        {
            var message = "";

            try
            {
                var feedback = _context.Feedbacks.First(a => a.feedbackId == id);

                feedback.isdeleted = true;
                feedback.deleted_at = DateTime.Now;
               _context.SaveChanges();
                message = "Success";
            }
            catch (Exception e)
            {
                throw e;
            }

            return message;
        }

        
        public string DeleteBroadcast(string batchId)
        {
            var message = "";

            try
            {
                var feedbacksToDelete = _context.Feedbacks.Where(a => a.batchId == batchId).ToList();

                foreach (var feedback in feedbacksToDelete)
                {
                    feedback.isdeleted = true;
                    feedback.deleted_at = DateTime.Now;
                }

                _context.SaveChanges();
                message = "Success";
            }
            catch (Exception e)
            {
                throw e;
            }

            return message;
        }

        public FeedbackMessage InsertUpdateFeedbackMessage(FeedbackMessage feedbackMessage)
        {

            bool insertMode = feedbackMessage.feedbackMessageId == 0;

            try
            {
                if (feedbackMessage != null)
                {
                    if (insertMode)
                    {
                        feedbackMessage.created_at = DateTime.Now;
                        feedbackMessage.updated_at = DateTime.Now;
                        feedbackMessage.isdeleted = false;
                        feedbackMessage.deleted_at = null;
                       
                        _context.FeedbackMessages.Add(feedbackMessage);
                    }
                    else
                    {
                        var local = _context.Set<FeedbackMessage>()
                    .Local
                    .FirstOrDefault(f => f.feedbackMessageId == feedbackMessage.feedbackMessageId);

                        if (local != null)
                        {
                            _context.Entry(local).State = EntityState.Detached;
                        }
                        _context.Entry(feedbackMessage).State = EntityState.Modified;
                    }

                    _context.SaveChanges();
                }

            }
            catch (Exception err)
            {
                string errMessage = err.Message;
                // Write to log

                throw;
            }

            return (feedbackMessage);
        }

        public Feedback GetFeedbackById(int Id)
        {
            Feedback feedback = new Feedback();

            try
            {
                feedback = _context.Feedbacks.Where(d => d.feedbackId == Id)
                .Include(f => f.FeedbackMessages)
                .ThenInclude(fm => fm.DocFeedbacks)
                .FirstOrDefault();
            }
            catch (Exception err)
            {

            }

            return (feedback);
        }

        public List<FeedbackMessage> GetBroadcastMessages()
        {
            List<FeedbackMessage> toReturn;

            try
            {
                var allMessages = _context.FeedbackMessages
                    .Where(d => d.isdeleted == false && d.broadcast != null)
                    .ToList();

                toReturn = allMessages
                    .GroupBy(d => d.broadcastId)
                    .Select(group => group.First())
                    .ToList();
                
            }
            catch (Exception err)
            {
                throw;
            }
            return toReturn;
        }

        public IEnumerable<Feedback> GetFeedbackMessagesBySenderId(string id)
        {
            IEnumerable<Feedback> feedbacks;

            try
            {
                feedbacks = _context.Feedbacks
                                        .Where(fm => fm.senderId == id)
                                        .OrderByDescending(d => d.feedbackId)
                                        .ToList();
            }
            catch (Exception err)
            {
                feedbacks = Enumerable.Empty<Feedback>();
            }

            return feedbacks;
        }

        public string PostInsertNewAdvert(Advert advert)
        {
            var message = "";
               
            if (advert.advertId == 0)
            {
                try
                {
                    advert.created_at = DateTime.Now;
                    advert.updated_at = DateTime.Now;
                    advert.isdeleted = false;

                    _context.Adverts.Add(advert);
                    _context.SaveChanges();
                    message = "Success";
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                advert.updated_at = DateTime.Now;
                advert.isdeleted = false;

                _context.Adverts.Update(advert);
                _context.SaveChanges();
                message = "Success";
            }

            return message;
        }

        public DocAdvert InsertUpdateDocAdvert(DocAdvert item)
        {
            bool insertMode = item.Id == 0;

            try
            {
                if (item != null)
                {
                    var clpExist = _context.DocAdverts.FirstOrDefault(f => (f.advertId == item.advertId) && (f.DocTypeName == item.DocTypeName));

                    if (clpExist != null)
                        insertMode = item.Id == 0;

                    if (insertMode)
                    {   

                        _context.DocAdverts.Add(item);
                    }
                    else
                    {

                        item.isdeleted = false;                        
                        
                        var local = _context.Set<DocAdvert>()
                        .Local
                        .FirstOrDefault(f => (f.advertId == item.advertId) && (f.DocTypeName == item.DocTypeName));

                        if (local != null)
                        {
                            _context.Entry(local).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }

                    _context.SaveChanges();

                }
            }
            catch (Exception err)
            {
                string errMessage = err.Message;
                throw;
            }

            return (item);
        }

        public DocFeedback InsertUpdateDocFeedback(DocFeedback item)
        {
            bool insertMode = item.Id == 0;

            try
            {
                if (item != null)
                {
                    var clpExist = _context.DocAdverts.FirstOrDefault(f => (f.advertId == item.feedbackMessageId) && (f.DocTypeName == item.DocTypeName));

                    if (clpExist != null)
                        insertMode = item.Id == 0;

                    if (insertMode)
                    {   

                        _context.DocFeedbacks.Add(item);
                    }
                    else
                    {

                        item.isdeleted = false;                        
                        
                        var local = _context.Set<DocFeedback>()
                        .Local
                        .FirstOrDefault(f => (f.feedbackMessageId == item.feedbackMessageId) && (f.DocTypeName == item.DocTypeName));

                        if (local != null)
                        {
                            _context.Entry(local).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }

                    _context.SaveChanges();

                }
            }
            catch (Exception err)
            {
                string errMessage = err.Message;
                throw;
            }

            return (item);
        }

        public DocAdvert GetDocAdvertFileById(int Id)
        {
            DocAdvert item = new DocAdvert();

            try 
            {
                item = _context.DocAdverts.Where(d => d.Id == Id).FirstOrDefault();
            } 
            catch (Exception err) 
            { 

            }            

            return (item);
        }

        public string DeleteDocAdvert(int id)
        {
            var message = "";

            try
            {
                var doc = _context.DocAdverts.First(a => a.Id == id);

                doc.isdeleted = true;
                doc.deleted_at = DateTime.Now;

                _context.SaveChanges();
                message = "Success";
            }
            catch (Exception e)
            {
                throw e;
            }

            return message;

        }

        public string DeleteAdvert(int id)
        {
            var message = "";

            try
            {
                var advert = _context.Adverts.First(a => a.advertId == id);

                advert.isdeleted = true;
                advert.deleted_at = DateTime.Now;

               _context.SaveChanges();
                message = "Success";
            }
            catch (Exception e)
            {
                throw e;
            }
            return message;
        }

        public string PostInsertSubcription(Subscription subscription)
        {
            var message = "";
 
            if (subscription.subscriptionId == 0)
            {
                try
                {
                    subscription.created_at = DateTime.Now;
                    subscription.updated_at = DateTime.Now;
                    subscription.isdeleted = false;

                    _context.Subscriptions.Add(subscription);
                    _context.SaveChanges();
                    message = "Success";
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                subscription.updated_at = DateTime.Now;
                subscription.isdeleted = false;

                _context.Subscriptions.Update(subscription);
                _context.SaveChanges();
                message = "Success";
            }

            return message;
        }

        public string DeleteSubscription(int id)
        {
            var message = "";

            try
            {
                var subscription = _context.Subscriptions.First(a => a.subscriptionId == id);

                subscription.isdeleted = true;
                subscription.deleted_at = DateTime.Now;
               _context.SaveChanges();
                message = "Success";
            }
            catch (Exception e)
            {
                throw e;
            }

            return message;
        }

        public Subscription GetSubscriptionById(int Id)
        {
            Subscription subscription = new Subscription();

            try
            {
                subscription = _context.Subscriptions.Where(d => d.subscriptionId == Id)
                .FirstOrDefault();
            }
            catch (Exception err)
            {

            }

            return (subscription);
        }

        public List<Package> GetAllPackages()
        {
            IQueryable<Package> toReturn;

            try
            {
                toReturn = _context.Packages.AsQueryable();
            }
            catch (Exception err)
            {
                throw;
            }
            return toReturn.ToList();
        }
        
        public List<Service> GetServicesByPackageId(int id)
        {
            List<Service> toReturn = new List<Service>();

            try
            {
                toReturn = _context.Services
                           .Where(d => d.packageId==id && d.isdeleted == false) 
                           .ToList();
            }
            catch (Exception err)
            {
                throw;
            }
            return toReturn;
        }

        public List<ServiceProduct> GetServiceProductsByServiceId(int id)
        {
            List<ServiceProduct> toReturn = new List<ServiceProduct>();

            try
            {
                toReturn = _context.ServiceProducts
                           .Where(d => d.serviceId==id && d.isdeleted == false) 
                           .ToList();
            }
            catch (Exception err)
            {
                throw;
            }
            return toReturn;
        }

        public List<Advert> GetAllAdverts()
        {
            IQueryable<Advert> toReturn;

            try
            {
                toReturn = _context.Adverts
                 .Where(d => d.isdeleted == false && d.ispublished == true)
                .AsQueryable();
            }
            catch (Exception err)
            {
                throw;
            }
            return toReturn.ToList();
        }


    }
}
