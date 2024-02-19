using Microsoft.EntityFrameworkCore;
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

        public DBLogic(ApplicationDbContext applicationDbContext)
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

            if (feedback.Id == 0)
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

        public string DeleteFeedback(int id)
        {
            var message = "";

            try
            {
                var feedback = _context.Feedbacks.First(a => a.Id == id);

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

        public FeedbackMessage InsertUpdateFeedbackMessage(FeedbackMessage feedback)
        {

            bool insertMode = feedback.Id == 0;

            try
            {
                if (feedback != null)
                {
                    if (insertMode)
                    {
                        feedback.created_at = DateTime.Now;
                        feedback.updated_at = DateTime.Now;
                        feedback.isdeleted = false;
                        feedback.deleted_at = null;
                       
                        _context.FeedbackMessages.Add(feedback);
                    }
                    else
                    {
                        var local = _context.Set<FeedbackMessage>()
                    .Local
                    .FirstOrDefault(f => f.Id == feedback.Id);

                        if (local != null)
                        {
                            _context.Entry(local).State = EntityState.Detached;
                        }
                        _context.Entry(feedback).State = EntityState.Modified;
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

            return (feedback);
        }

        public FeedbackMessage GetFeedbackMessageById(int Id)
        {
            FeedbackMessage feedback = new FeedbackMessage();

            try
            {
                feedback = _context.FeedbackMessages.Where(d => d.Id == Id).FirstOrDefault();
            }
            catch (Exception err)
            {

            }


            return (feedback);
        }

        public FeedbackMessage GetFeedbackMessagesBySubcriberId(string Id)
        {
            FeedbackMessage feedback = new FeedbackMessage();

            try
            {
                feedback = _context.FeedbackMessages.Where(d => d.SubcriberId == Id).FirstOrDefault();
            }
            catch (Exception err)
            {

            }

            return (feedback);
        }

    }
}
