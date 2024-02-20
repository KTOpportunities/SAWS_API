using Microsoft.EntityFrameworkCore;
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

namespace SAWSCore3API.Logic
{
    public class DBLogic
    {
        public ApplicationDbContext _context;

        public DBLogic( ApplicationDbContext applicationDbContext)
        {
            _context = applicationDbContext;
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
            
            if (feedback.feebackId == 0)
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


        public string DeleteFeedback(int id)
        {
            var message = "";

            try
            {
                var feedback = _context.Feedbacks.First(a => a.feebackId == id);

                feedback.isdeleted = true;
                feedback.deleted_at = DateTime.Now;

                // foreach (var feedbackMessage in feedback.FeedbackMessages)
                // {
                //     feedbackMessage.deleted_at = DateTime.Now;
                //     feedbackMessage.isdeleted = true;
                // }

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
                feedback = _context.Feedbacks.Where(d => d.feebackId == Id)
                .Include(f => f.FeedbackMessages)
                .FirstOrDefault();
            }
            catch (Exception err)
            {

            }

            return (feedback);
        }

        public IEnumerable<FeedbackMessage> GetFeedbackMessagesBySenderId(string id)
        {
            IEnumerable<FeedbackMessage> feedbackMessages;

            try
            {
                feedbackMessages = _context.FeedbackMessages
                                        .Where(fm => fm.senderId == id)
                                        .OrderByDescending(d => d.feedbackMessageId)
                                        .ToList();
            }
            catch (Exception err)
            {
                feedbackMessages = Enumerable.Empty<FeedbackMessage>();
            }

            return feedbackMessages;
        }


    }
}
