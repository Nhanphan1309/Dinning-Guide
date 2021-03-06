﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using Dinning_Guide.Models.User;
using Dinning_Guide.Models.Restaurant;
using Dinning_Guide.Models.Rate;
using PagedList;
using System.Web.Security;
using System.Data;
using System.Data.Entity.Infrastructure;

namespace Dinning_Guide.Controllers
{
    public class HomeController : Controller
    {
        private DB_Entities _db = new DB_Entities();
        private Db_Rates db2 = new Db_Rates();
        private Db_Restaurants db1 = new Db_Restaurants();
        // GET: Home
        public ActionResult Index(string search)
        {
            if (Session["idUser"] != null)
            {
                if (search != null) return RedirectToAction("Index1", new { option = "Name", search = search, pageNumber = 1, sort = "descending name" });
                return View();

            }
            else
            {
                //return RedirectToAction("Login");
                if (search != null) return RedirectToAction("Index1", new { option = "Name", search = search, pageNumber = 1, sort = "descending name" });
                return View();

            }
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        //GET: Register
        public ActionResult Register()
        {
            return View();
        }

        //POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Models.User.User _user)
        {
            if (ModelState.IsValid)
            {
                var check = _db.Users.FirstOrDefault(s => s.Email == _user.Email);
                if (check == null)
                {
                    _user.Password = GetMD5(_user.Password);
                    _user.idUser++;
                    _user.Type = 0;
                    _db.Configuration.ValidateOnSaveEnabled = true;
                    _db.Users.Add(_user);
                    _db.SaveChanges();
                    return RedirectToAction("Index","Home");
                }
                else
                {
                    ViewBag.error = "Email already exists";
                    return View();
                }
            }
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if (ModelState.IsValid)
            {

                var f_password = GetMD5(password);
                var data = _db.Users.Where(s => s.Email.Equals(email) && s.Password.Equals(f_password)).ToList();
                if (data.Count() > 0)
                {
                    //add session
                    Session["FullName"] = data.FirstOrDefault().FirstName + " " + data.FirstOrDefault().LastName;
                    Session["Email"] = data.FirstOrDefault().Email;
                    Session["idUser"] = data.FirstOrDefault().idUser;
                    Session["Type"] = data.FirstOrDefault().Type;
                    return RedirectToAction("Index","Home");
                }
                else
                {
                    ViewBag.error = "Login failed";
                    return RedirectToAction("Login","Home");
                }
            }
            return View();
        }

        //Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();//remove session
            return RedirectToAction("Login","Home");
        }

        public ActionResult Profile()
        {
            return View();
        }


        //create a string MD5
        public static string GetMD5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] fromData = Encoding.UTF8.GetBytes(str);
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = null;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x2");

            }
            return byte2String;
        }

        /// ---------------------------------------------------------

        public ActionResult Index1(string option, string search, double? rate, int? pageNumber, string sort)
        {
            //if the sort parameter is null or empty then we are initializing the value as descending name  
            ViewBag.SortByName = string.IsNullOrEmpty(sort) ? "descending name" : "";
            //if the sort value is gender then we are initializing the value as descending gender  
            ViewBag.SortByDescription = sort == "Description" ? "descending description" : "Description";

            //here we are converting the Db1 Restaurant to AsQueryable => we can invoke all the extension methods on variable records.  
            var records = db1.Restaurants.AsQueryable();
            var rates = db2.Rates.AsQueryable();
            string temp ="0";
            try
            {
                double test = Convert.ToDouble(search);
                temp = String.Copy(search);
            }
            catch(Exception ex)
            {
                temp = "0";
            }
            double number = Convert.ToDouble(temp);//Incase of null value
            //if a user choose the radio button option as Description  

            if (option == "Address")
            {
                records = records.Where(x => x.Address.Contains(search) || search == null);
            }
            else if (option == "Description")
            {
                records = records.Where(x => x.Decription.Contains(search)|| search == null);
            }
            else if (option == "Rate")
            {
                records = records.Where(x => x.Rate == number || search == null);
            }

            else
            {
                records = records.Where(x => x.Name.Contains(search) || search == null);
            }
            foreach(var item in records)
            {
                try
                {
                    var rating = rates.Where(x => x.IDRestaurant == item.ID).Select(x => x.Rate1).Average();
                    rating = System.Math.Round(rating, 2);
                    item.Rate = rating;
                    db1.Configuration.ValidateOnSaveEnabled = true;
                }
                catch (Exception ex)
                {
                    item.Rate = 0;
                    db1.Configuration.ValidateOnSaveEnabled = true;
                }
            }
            db1.SaveChanges();

            switch (sort)
            {

                case "descending name":
                    records = records.OrderByDescending(x => x.Name);
                    break;

                case "descending rate":
                    records = records.OrderByDescending(x => x.Rate);
                    break;

                case "Address":
                    records = records.OrderBy(x => x.Address);
                    break;

                default:
                    records = records.OrderBy(x => x.Name);
                    break;

            }

            return View(records.ToPagedList(pageNumber ?? 1, 100));
        }

        ///LOGOUT///---------------------------------------------
        public ActionResult ViewProfile()
        {
            return RedirectToAction("Index1","Home");
        }

        ///ADD REVIEW ///-----------------------------------------
       
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateReview(Models.Rate.Rate rate, int? id)
        {

            if (ModelState.IsValid)
            {    
                try
                {
                    var checkId = Session["idUser"];
                    if ((int)id==null||(int)checkId==null) return RedirectToAction("Index1", "Home");
                    //Should catch the exception if object is null or it will always be true 
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException){
                    return RedirectToAction("Index1", "Home");
                }
                var userId = Session["idUser"];
                rate.IDRestaurant = (int)id;
                rate.IDUser = (int)userId;
                rate.IDReview++;
                db2.Configuration.ValidateOnSaveEnabled = true;
                db2.Rates.Add(rate);
                db2.SaveChanges();
                return RedirectToAction("Index1", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Unable to save changes.Try again.");
                return View();
            }
            return View();
        }
        public ActionResult CreateReview()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int? id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    if ((int)id == null || (int)checkId == null) return RedirectToAction("Index1", "Home");
                    //Should catch the exception if object is null or it will always be true 
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index1", "Home");
                }
                var userId = Session["idUser"];
                var rates = db2.Rates.AsQueryable();
                if((int)id != null)
{
                    rates = rates.Where(s => s.IDRestaurant == (int)id);
                }
                if ((int)userId != null)
                {
                    rates = rates.Where(s => s.IDUser == (int)userId);
                }
                foreach (var item in rates)
                {
                    //db2.Rates.Find(rate);
                    db2.Configuration.ValidateOnSaveEnabled = true;
                    db2.Rates.Remove(item);
                }
                db2.SaveChanges();
                return RedirectToAction("Index1","Home");
            }
            else
            {
                return View();
            }
            return View();
        }
        public ActionResult Delete()
        {
            return View();
        }

        ///View Detail-------------------
        public ActionResult Details(int? id)
        {
            Db_Restaurants db1 = new Db_Restaurants();
            if (id == null)
            {
                return RedirectToAction("Index1","Home");
            }
            Restaurant restaurant = db1.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
            ViewBag.restaurantId = (int)id;
            
            return View(restaurant);
        }

        public ActionResult ReviewDetail(int? id,int? pageNumber)
        {
            Db_Rates db2 = new Db_Rates();
            if (id == null)
            {
                return RedirectToAction("Index1", "Home");
            }
            var rates = db2.Rates.AsQueryable();
            ViewBag.restaurantId = (int)id;
            rates = rates.OrderBy(x => x.IDRestaurant == (int)id);
            rates = rates.Where(x => x.IDRestaurant == (int)id);
            return View(rates.ToPagedList(pageNumber ?? 1, 100));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewDelete(int? id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    var typeId = Session["Type"];
                    if ((int)id == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                    //Should catch the exception if object is null or it will always be true
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index", "Home");
                }
                var rates = db2.Rates.AsQueryable();
                if ((int)id != null)
                {
                    rates = rates.Where(s => s.IDReview == (int)id);
                }
                foreach (var item in rates)
                {
                    //db2.Rates.Find(rate);
                    db2.Configuration.ValidateOnSaveEnabled = true;
                    db2.Rates.Remove(item);
                }
                db2.SaveChanges();
                return RedirectToAction("Index1", "Home");
            }
            else
            {
                return View();
            }
            return View();
        }

        public ActionResult ReviewDelete()
        {
            return View();
        }

        public ActionResult ReviewEdit(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            Rate rate = db2.Rates.Find(id);
            if (rate == null)
            {
                return HttpNotFound();
            }
            return View(rate);
        }

        [HttpPost, ActionName("ReviewEdit")] //Ha ha good luck finding this shit on stackoverflow jesus fucking christ my brain cells
        [ValidateAntiForgeryToken]
        public ActionResult ReviewEditPost(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var reviewToUpdate = db2.Rates.Find(id);
            if (TryUpdateModel(reviewToUpdate, "",
               new string[] { "Rate1", "Review"}))
            {
                try
                {
                    db2.SaveChanges();

                    return RedirectToAction("Index", "Home");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(reviewToUpdate);

        }

        public ActionResult ReviewManage(int? pageNumber)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            var userId = Session["idUser"];
            var rates = db2.Rates.AsQueryable();
            rates = rates.OrderBy(x => x.IDUser == (int)userId);
            rates = rates.Where(x => x.IDUser == (int)userId);
            return View(rates.ToPagedList(pageNumber ?? 1, 100));
        }

        public ActionResult AReviewManage(string option, string search, int? pageNumber, string sort)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId!=2) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            //if the sort parameter is null or empty then we are initializing the value as descending name  
            ViewBag.SortByName = string.IsNullOrEmpty(sort) ? "descending name" : "";
            //if the sort value is gender then we are initializing the value as descending gender  
            ViewBag.SortByDescription = sort == "Description" ? "descending description" : "Description";

            //here we are converting the Db1 Restaurant to AsQueryable => we can invoke all the extension methods on variable records.  
            var rates = db2.Rates.AsQueryable();
            string temp = "0";
            try
            {
                int test = Convert.ToInt32(search);
                temp = String.Copy(search);
            }
            catch (Exception ex)
            {
                temp = "0";
            }
            int number = Convert.ToInt32(temp);//Incase of null value

            //if a user choose the radio button option as Description  

            if (option == "IDReview")
            {
                rates = rates.Where(x => x.IDReview == number || search == null);
            }
            else if (option == "IDRestaurant")
            {
                rates = rates.Where(x => x.IDRestaurant==number || search == null);
            }
            else if (option == "IDUser")
            {
                rates = rates.Where(x => x.IDUser == number || search == null);
            }
            else if (option == "Rating")
            {
                rates = rates.Where(x => x.Rate1 == number || search == null);
            }
            else
            {
                rates = rates.Where(x => x.Review.Contains(search) || search == null);
            }

            switch (sort)
            {

                case "descending name":
                    rates = rates.OrderByDescending(x => x.IDReview);
                    break;

                case "descending rate":
                    rates = rates.OrderByDescending(x => x.Rate1);
                    break;

                case "Address":
                    rates = rates.OrderBy(x => x.IDRestaurant);
                    break;

                default:
                    rates = rates.OrderBy(x => x.IDReview);
                    break;

            }

            return View(rates.ToPagedList(pageNumber ?? 1, 100));
        }

        public ActionResult ORestaurantManage(int? pageNumber)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId != 1) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch(System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            var userId = Session["idUser"];
            var records = db1.Restaurants.AsQueryable();
            var rates = db2.Rates.AsQueryable();
            records = records.OrderBy(x => x.IDUser == (int)userId);
            records = records.Where(x => x.IDUser == (int)userId);
            foreach (var item in records)
            {
                try
                {
                    var rating = rates.Where(x => x.IDRestaurant == item.ID).Select(x => x.Rate1).Average();
                    rating = System.Math.Round(rating, 2);
                    item.Rate = rating;
                    db1.Configuration.ValidateOnSaveEnabled = true;
                }
                catch (Exception ex)
                {
                    item.Rate = 0;
                    db1.Configuration.ValidateOnSaveEnabled = true;
                }
            }
            db1.SaveChanges();
            return View(records.ToPagedList(pageNumber ?? 1, 100));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ORestaurantCreate(Models.Restaurant.Restaurant restaurant)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    var typeId = Session["Type"];
                    if ((int)typeId == null || (int)checkId == null || (int)typeId != 1) return RedirectToAction("Index", "Home");
                    //Should catch the exception if object is null or it will always be true 
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index", "Home");
                }
                var userId = Session["idUser"];
                restaurant.IDUser = (int)userId;
                restaurant.Rate = 0;
                restaurant.ID++;
                db1.Configuration.ValidateOnSaveEnabled = true;
                db1.Restaurants.Add(restaurant);
                db1.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Unable to save changes.Try again.");
                return View();
            }
            return View();
        }
        public ActionResult ORestaurantCreate()
        {
            return View();
        }

        public ActionResult RestaurantEdit(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            Restaurant restaurant = db1.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
            return View(restaurant);
        }

        [HttpPost,ActionName("RestaurantEdit")] //Ha ha good luck finding this shit on stackoverflow jesus fucking christ my brain cells
        [ValidateAntiForgeryToken]
        public ActionResult RestaurantEditPost(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index","Home");
            }
            var restaurantToUpdate = db1.Restaurants.Find(id);
            if (TryUpdateModel(restaurantToUpdate, "",
               new string[] { "Name", "Address", "Decription" }))
            {
                try
                {
                    db1.SaveChanges();

                    return RedirectToAction("Index","Home");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(restaurantToUpdate);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ORestaurantDelete(int? id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    var typeId = Session["Type"];
                    if ((int)typeId == null || (int)checkId == null || (int)typeId != 1 || (int) id == null) return RedirectToAction("Index", "Home");
                    //Should catch the exception if object is null or it will always be true 
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index", "Home");
                }
                var userId = Session["idUser"];
                var restaurant = db1.Restaurants.AsQueryable();
                var rate = db2.Rates.AsQueryable();
                if ((int)userId != null)
                {
                    restaurant = restaurant.Where(s => s.IDUser == (int)userId);
                }
                if((int)id != null)
                {
                    restaurant = restaurant.Where(s => s.ID == (int)id);
                }
                foreach (var item in restaurant)
                {
                    //db2.Rates.Find(rate);
                    db1.Configuration.ValidateOnSaveEnabled = true;
                    db1.Restaurants.Remove(item);
                }
                if ((int)id != null)
                {
                    rate = rate.Where(s => s.IDRestaurant == (int)id);
                }
                foreach (var item in rate)
                {
                    //db2.Rates.Find(rate);
                    db2.Configuration.ValidateOnSaveEnabled = true;
                    db2.Rates.Remove(item);
                }
                db2.SaveChanges();
                db1.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }
            return View();
        }
        public ActionResult ORestaurantDelete()
        {
            return View();
        }

        public ActionResult ARestaurantManage(string option, string search, double? rate, int? pageNumber, string sort)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null ||(int)typeId != 2) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            //if the sort parameter is null or empty then we are initializing the value as descending name  
            ViewBag.SortByName = string.IsNullOrEmpty(sort) ? "descending name" : "";
            //if the sort value is gender then we are initializing the value as descending gender  
            ViewBag.SortByDescription = sort == "Description" ? "descending description" : "Description";

            //here we are converting the Db1 Restaurant to AsQueryable => we can invoke all the extension methods on variable records.  
            var records = db1.Restaurants.AsQueryable();
            var rates = db2.Rates.AsQueryable();
            string temp = "0";
            try
            {
                double test = Convert.ToDouble(search);
                temp = String.Copy(search);
            }
            catch (Exception ex)
            {
                temp = "0";
            }
            double number = Convert.ToDouble(temp);//Incase of null value

            //if a user choose the radio button option as Description  

            if (option == "Address")
            {
                records = records.Where(x => x.Address.Contains(search) || search == null);
            }
            else if (option == "Description")
            {
                records = records.Where(x => x.Decription.Contains(search) || search == null);
            }
            else if (option == "Rate")
            {
                records = records.Where(x => x.Rate == number || search == null);
            }

            else
            {
                records = records.Where(x => x.Name.Contains(search) || search == null);
            }
            foreach (var item in records)
            {
                try
                {
                    var rating = rates.Where(x => x.IDRestaurant == item.ID).Select(x => x.Rate1).Average();
                    rating = System.Math.Round(rating, 2);
                    item.Rate = rating;
                    db1.Configuration.ValidateOnSaveEnabled = true;
                }
                catch (Exception ex)
                {
                    item.Rate = 0;
                    db1.Configuration.ValidateOnSaveEnabled = true;
                }
            }
            db1.SaveChanges();

            switch (sort)
            {

                case "descending name":
                    records = records.OrderByDescending(x => x.Name);
                    break;

                case "descending rate":
                    records = records.OrderByDescending(x => x.Rate);
                    break;

                case "Address":
                    records = records.OrderBy(x => x.Address);
                    break;

                default:
                    records = records.OrderBy(x => x.Name);
                    break;

            }

            return View(records.ToPagedList(pageNumber ?? 1, 100));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ARestaurantDelete(int? id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    var typeId = Session["Type"];
                    if ((int)id == null || (int)checkId == null || (int)typeId != 2) return RedirectToAction("Index", "Home");
                    //Should catch the exception if object is null or it will always be true
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index", "Home");
                }
                var userId = Session["idUser"];
                var restaurant = db1.Restaurants.AsQueryable();
                if ((int)id != null)
                {
                    restaurant = restaurant.Where(s => s.ID == (int)id);
                }
                foreach (var item in restaurant)
                {
                    //db2.Rates.Find(rate);
                    db1.Configuration.ValidateOnSaveEnabled = true;
                    db1.Restaurants.Remove(item);
                }
                var rates = db2.Rates.AsQueryable();
                if ((int)id != null)
                {
                    rates = rates.Where(s => s.IDRestaurant == (int)id);
                }
                foreach (var item in rates)
                {
                    //db2.Rates.Find(rate);
                    db2.Configuration.ValidateOnSaveEnabled = true;
                    db2.Rates.Remove(item);
                }
                db2.SaveChanges();
                db1.SaveChanges();
                return RedirectToAction("ARestaurantManage", "Home");
            }
            else
            {
                return View();
            }
            return View();
        }
        public ActionResult ARestaurantDelete()
        {
            return View();
        }

        public ActionResult AUserManage(string option, string search, int? pageNumber, string sort)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId != 2) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            //if the sort parameter is null or empty then we are initializing the value as descending name  
            ViewBag.SortByName = string.IsNullOrEmpty(sort) ? "descending name" : "";
            //if the sort value is gender then we are initializing the value as descending gender  
            ViewBag.SortByDescription = sort == "Description" ? "descending description" : "Description";

            //here we are converting the Db1 Restaurant to AsQueryable => we can invoke all the extension methods on variable records.  
            var records = _db.Users.AsQueryable();

            //if a user choose the radio button option as Description
            string temp = "0";
            try
            {
                int test = Convert.ToInt32(search);
                temp = String.Copy(search);
            }
            catch (Exception ex)
            {
                temp = "0";
            }
            int number = Convert.ToInt32(temp);//incase some funny man decide to search for null
            if (option == "idUser")
            {
                records = records.Where(x => x.idUser== number || search == null);
            }
            else if (option == "FirstName")
            {
                records = records.Where(x => x.FirstName.Contains(search) || search == null);
            }
            else if (option == "LastName")
            {
                records = records.Where(x => x.LastName.Contains(search) || search == null);
            }
            else if (option == "Type")
            {
                records = records.Where(x => x.Type == number || search == null);
            }
            else
            {
                records = records.Where(x => x.Email.Contains(search) || search == null);
            }

            switch (sort)
            {

                case "descending name":
                    records = records.OrderByDescending(x => x.idUser);//Dumb dumb LinQ doesn't support int.parse stoopid so no sorting with int :)...FOR NOW !
                    break;

                case "descending rate":
                    records = records.OrderByDescending(x => x.FirstName);
                    break;

                case "Address":
                    records = records.OrderBy(x => x.LastName);
                    break;

                default:
                    records = records.OrderBy(x => x.Email);
                    break;

            }

            return View(records.ToPagedList(pageNumber ?? 1, 100));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AUserDelete(int? id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    var typeId = Session["Type"];
                    if ((int)id == null || (int)checkId == null || (int)typeId != 2) return RedirectToAction("Index", "Home");
                    //Should catch the exception if object is null or it will always be true
                }
                catch (System.NullReferenceException)
                {
                    return RedirectToAction("Index", "Home");
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index", "Home");
                }
                var userId = (int)id;
                var restaurant = db1.Restaurants.AsQueryable();
                if ((int)id != null)
                {
                    restaurant = restaurant.Where(s => s.IDUser == (int)id);
                }
                foreach (var item in restaurant)
                {
                    //db2.Rates.Find(rate);
                    db1.Configuration.ValidateOnSaveEnabled = true;
                    db1.Restaurants.Remove(item);
                }
                var rates = db2.Rates.AsQueryable();
                if ((int)id != null)
                {
                    rates = rates.Where(s => s.IDUser == (int)id);
                }
                foreach (var item in rates)
                {
                    //db2.Rates.Find(rate);
                    db2.Configuration.ValidateOnSaveEnabled = true;
                    db2.Rates.Remove(item);
                }
                var user = _db.Users.AsQueryable();
                if ((int)id != null)
                {
                    user = user.Where(s => s.idUser == (int)id);
                }
                foreach (var item in user)
                {
                    //db2.Rates.Find(rate);
                    _db.Configuration.ValidateOnSaveEnabled = true;
                    _db.Users.Remove(item);
                }
                db2.SaveChanges();
                db1.SaveChanges();
                _db.SaveChanges();
                return RedirectToAction("AUserManage", "Home");
            }
            else
            {
                return View();
            }
            return View();
        }
        public ActionResult AUserDelete()
        {
            return View();
        }

        public ActionResult AUserEdit(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId != 2) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            User user = _db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("AUserEdit")] //Ha ha good luck finding this shit on stackoverflow jesus fucking christ my brain cells
        [ValidateAntiForgeryToken]
        public ActionResult AUserEditPost(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId != 2) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var userToUpdate = _db.Users.Find(id);
            if (TryUpdateModel(userToUpdate, "",
               new string[] { "FirstName", "LastName","Type" }))
            {
                try
                {
                    _db.SaveChanges();

                    return RedirectToAction("Index", "Home");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(userToUpdate);

        }

        public ActionResult UserEdit(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            User user = _db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("UserEdit")] //Ha ha good luck finding this shit on stackoverflow jesus fucking christ my brain cells
        [ValidateAntiForgeryToken]
        public ActionResult UserEditPost(int? id)
        {
            try
            {
                var checkId = Session["idUser"];
                var typeId = Session["Type"];
                if ((int)typeId == null || (int)checkId == null || (int)typeId == 0) return RedirectToAction("Index", "Home");
                //Should catch the exception if object is null or it will always be true 
            }
            catch (System.NullReferenceException)
            {
                return RedirectToAction("Index", "Home");
            }
            catch (System.InvalidOperationException)
            {
                return RedirectToAction("Index", "Home");
            }
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var userToUpdate = _db.Users.Find(id);
            if (TryUpdateModel(userToUpdate, "",
               new string[] { "FirstName", "LastName"}))
            {
                try
                {
                    _db.SaveChanges();
                    Session["FullName"] = userToUpdate.FirstName + " " + userToUpdate.LastName;
                    return RedirectToAction("Index", "Home");
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(userToUpdate);

        }

        ///CREATE RESTAURANT--------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRestaurant(Models.Restaurant.Restaurant restaurant, int? id)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    var checkId = Session["idUser"];
                    if ((int)id == null || (int)checkId == null) return RedirectToAction("Index1", "Home");
                    //Should catch the exception if object is null or it will always be true 
                }
                catch (System.InvalidOperationException)
                {
                    return RedirectToAction("Index1", "Home");
                }
                var userId = Session["idUser"];
                restaurant.ID = (int)id;
                restaurant.IDUser = (int)userId;
                restaurant.ID++;
                db1.Configuration.ValidateOnSaveEnabled = true;
                db1.Restaurants.Add(restaurant);
                db1.SaveChanges();
                return RedirectToAction("Index1", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Unable to save changes.Try again.");
                return View();
            }
            return View();
        }
        public ActionResult CreateRestaurant()
        {
            return View();
        }
    }
}