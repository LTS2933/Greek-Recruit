# GreekRecruit 
A streamlined web app designed to help Greek organizations and clubs alike manage their recruitment process, track/discuss PNMs (Potential New Members), and facilitate voting and event planning in an organized manner.

## Features

 **User Authentication**
  - Secure login system with role-based access (Admins vs Regular Users)
  - Different role-based features and functionalities 
  
  **PNM Management**
  - Add individual PNMs or import a batch from CSV
  - View, edit, and update PNM information
  - Profile pictures, GPA, phone number, Instagram handle, and more

  **Voting Sessions**
  - Admins can open and close voting sessions for PNMs
  - Members can vote anonymously via a simple Yes/No interface
  - Voting links are accessible through QR codes
  - Vote analytics show counts and percentages

  **Comment System**
  - Members can leave timestamped comments on PNMs
  - Great for sharing notes and impressions

  **Event Management**
  - Create and view upcoming chapter events
  - Include time, location, and descriptions

   **Apple-Specific SMS Shortcut**
  - Apple-based users see a "Send Message" button next to PNM phone numbers
  - Instantly opens the default messaging app to text a PNM

## Tech Stack

- **Frontend**: Razor Pages, Bootstrap 5
- **Backend**: C#, ASP.NET Core MVC
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: Cookie-based Auth with role differentiation
