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

    }
}
