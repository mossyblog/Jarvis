# Setting Up Your .NET Environment

Welcome! Let's get your computer ready for Jarvis ECS. We'll go step by step. If you get stuck, don't worry—there's help at the end of every section.

## What is .NET?
.NET is a framework for building apps. Jarvis ECS is built with .NET, so you need it installed.

## What you need:
- **Git**: Lets you download code from the internet.
- **.NET SDK 8.0 or later**: Lets you build and run .NET apps.
- **Docker Desktop**: Lets you run PostgreSQL database locally.

## Step-by-step:

1. **Install Git**
   - Go to [https://git-scm.com/download/win](https://git-scm.com/download/win)
   - Click the big **Download** button.
   - Open the file you downloaded. Click **Next** a lot (the defaults are fine).
   - When it finishes, click **Finish**.
   - To check it's installed: Open the **Start menu**, type `cmd`, and open **Command Prompt**. Type:
     ```
     git --version
     ```
     You should see something like `git version 2.40.1.windows.1`.

2. **Install .NET SDK**
   - Go to [https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download)
   - Click **Download .NET SDK x.y** (choose the latest version, e.g., 8.0).
   - Open the file you downloaded. Click **Install**.
   - When it finishes, click **Close**.
   - To check it's installed: In **Command Prompt**, type:
     ```
     dotnet --version
     ```
     You should see a number like `8.0.100`.

3. **Clone the Jarvis project**
   - In **Command Prompt**, go to the folder where you want the code (e.g., `C:\Code`):
     ```
     cd C:\Code
     ```
   - Run:
     ```
     git clone <your-jarvis-repo-url>
     cd jarvis
     ```
   - You should see a bunch of folders and files if it worked.

---

# Installing Scoop (Optional, but Recommended)

Scoop is a package manager for Windows. It's like Homebrew for Mac. Scoop makes it super easy to install and update developer tools (like git, docker, and more) with just a single command.

**Why use Scoop?**
- No more hunting for download links or clicking through installers.
- Scoop installs tools in your user folder, so you don't need admin rights (most of the time).
- Updating tools is as easy as one command.

## Step-by-step: Installing Scoop (Windows/PowerShell Only)

1. **Open PowerShell**
   - Press the **Start** button, type `PowerShell`, and click **Windows PowerShell**.
   - (Do NOT use Command Prompt for this step.)

2. **Enable script execution (required for Scoop)**
   - In PowerShell, type:
     ```
     Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
     ```
   - If it asks, type `Y` and press **Enter**.
   - This lets you run scripts you download (safe for your user only).

3. **Install Scoop (official method)**
   - In PowerShell, type:
     ```
     Invoke-RestMethod -Uri https://get.scoop.sh | Invoke-Expression
     ```
   - Wait for it to finish. You should see messages about Scoop being installed.

4. **Check Scoop is installed**
   - In PowerShell, type:
     ```
     scoop --version
     ```
   - You should see a version number, like `Scoop v1.0.0`.

## Using Scoop to install tools

- To install git:
  ```
  scoop install git
  ```
- To install docker (desktop):
  ```
  scoop bucket add extras
  scoop install docker-desktop
  ```

You can use Scoop to install many other tools. See [Scoop's app directory](https://scoop.sh/#/apps) for more.

## Troubleshooting Scoop
- If you see errors about "execution policy", make sure you ran the `Set-ExecutionPolicy` command above.
- If you get network errors, check your internet connection or try again later.
- If you see "Invoke-RestMethod is not recognized", make sure you are in PowerShell, not Command Prompt.
- For more help, see [Scoop's documentation](https://scoop.sh/).

**What to do if you get stuck:**
- Double-check you are using PowerShell, not Command Prompt.
- Copy and paste the commands exactly.
- If you get an error, read it carefully—sometimes it tells you exactly what to do.
- Ask a teammate or search online for the error message.

---

# Setting Up Docker and PostgreSQL

Docker lets you run software in a "container"—like a mini-computer inside your computer. Jarvis ECS uses Docker to run PostgreSQL database, so you don't have to install a database yourself.

## What is Docker?
- It's a tool for running apps in a safe, isolated way.
- You need it for PostgreSQL to work locally.

## Step-by-step:

1. **Install Docker Desktop**
   - **Option A: Download from Docker Website**
     - Go to [https://www.docker.com/products/docker-desktop/](https://www.docker.com/products/docker-desktop/)
     - Click **Download for Windows (Windows 10/11)**
     - Open the file you downloaded. Click **Yes** if Windows asks for permission.
     - Click **Next** a few times, then **Install**.
     - Wait for it to finish, then click **Finish**.
   - **Option B: Install from Microsoft Store**
     - Open the **Microsoft Store** app (search for it in the Start menu).
     - Search for "Docker Desktop".
     - Click **Get** or **Install**.
     - Wait for it to finish.

2. **Start Docker Desktop**
   - Open the **Start menu** and type "Docker Desktop". Click it.
   - Wait for the Docker whale icon to appear in your system tray (bottom right of your screen).
   - Hover over the icon. It should say "Docker Desktop is running".
   - If it asks to update WSL2 or install extra things, say **Yes** and follow the prompts.

3. **Verify Docker is working**
   - Open **Command Prompt**.
   - Type:
     ```
     docker --version
     ```
     You should see something like `Docker version 24.0.2, build ...`.
   - Type:
     ```
     docker info
     ```
     You should see lots of info about Docker.

4. **Configure Docker Desktop (Recommended)**
   - Open Docker Desktop.
   - Click the **gear icon** (Settings) in the top right.
   - Under **General**, make sure "Use the WSL 2 based engine" is checked (recommended for Windows 10/11).
   - Under **Resources > CPU & Memory**, you can adjust how much RAM/CPU Docker can use. (Default is fine for most.)
   - Click **Apply & Restart** if you change anything.

5. **Troubleshooting Docker**
   - If Docker won't start, restart your computer and try again.
   - If you see errors about WSL2, follow the prompts to install/update WSL2 ([WSL2 install guide](https://docs.microsoft.com/en-us/windows/wsl/install)).
   - If you get permission errors, right-click Docker Desktop and choose **Run as administrator**.
   - For more help, see the [Docker Desktop Troubleshooting Guide](https://docs.docker.com/desktop/troubleshoot/).

> **Docker must be running before you continue!**

---

# Running PostgreSQL Locally with Docker

Now you'll start your own PostgreSQL database on your computer using Docker Compose. This is where Jarvis ECS will store its data.

## Step-by-step:

1. **Navigate to your project folder**
   - In **Command Prompt**, make sure you're in your project folder (e.g., `C:\Code\jarvis`).
   - You should see a `docker-compose.yml` file when you type:
     ```
     dir docker-compose.yml
     ```

2. **Start PostgreSQL**
   - In the same folder, type:
     ```
     docker-compose up -d
     ```
   - The first time, this will download the PostgreSQL Docker image. It may take a few minutes.
   - When it's done, you'll see:
     ```
     Creating pg_graphql_db ... done
     ```

3. **Verify PostgreSQL is running**
   - Check the container status:
     ```
     docker ps
     ```
   - You should see a container named `pg_graphql_db` with status "Up".
   
   - The database is now accessible at:
     - **Host:** localhost
     - **Port:** 5432
     - **Database:** postgres
     - **Username:** postgres
     - **Password:** postgres

4. **If you need to stop PostgreSQL**
   - In the terminal, type:
     ```
     docker-compose down
     ```
   - This will stop the PostgreSQL container. You can start it again with `docker-compose up -d`.

5. **If you want to reset your database**
   - This will delete all your data! Only do this if you want a fresh start.
   - Type:
     ```
     docker-compose down -v
     ```
   - The `-v` flag removes the database volume, giving you a clean slate.

---

**What to do if you get stuck:**
- If you see errors about Docker, make sure Docker Desktop is running.
- If you see errors about port 5432 already in use, make sure no other PostgreSQL instance is running.
- If you get a permissions error, try running the terminal as Administrator.

---

# Managing PostgreSQL with Database Tools

You can use various tools to manage your PostgreSQL database. Here are some options:

## Option 1: Command Line (psql)
- Connect using Docker:
  ```
  docker exec -it pg_graphql_db psql -U postgres
  ```
- You'll see a `postgres=#` prompt where you can run SQL commands.
- Type `\q` to exit.

## Option 2: GUI Tools (Recommended)
Popular PostgreSQL GUI tools include:
- **pgAdmin** - [https://www.pgadmin.org/](https://www.pgadmin.org/)
- **DBeaver** - [https://dbeaver.io/](https://dbeaver.io/)
- **TablePlus** - [https://tableplus.com/](https://tableplus.com/)

Connection settings for any tool:
- **Host:** localhost
- **Port:** 5432
- **Database:** postgres
- **Username:** postgres
- **Password:** postgres

---

# Configuring Your .NET Application

Now you'll tell your .NET app how to connect to your local PostgreSQL database.

## Step-by-step:

1. **Update your connection string**
   - In your app's configuration file (appsettings.json or appsettings.Development.json), add or update:
     ```json
     {
       "ConnectionStrings": {
         "DefaultConnection": "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
       }
     }
     ```

2. **Configure your services**
   - In your app's startup code (Program.cs or Startup.cs), make sure your database context is configured:
     ```csharp
     services.AddDbContext<YourDbContext>(options =>
         options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
     ```

3. **Run database migrations (if applicable)**
   - If your project uses Entity Framework migrations:
     ```
     dotnet ef database update
     ```

4. **Save your changes**
   - Make sure to save all files before running your app.

---

# Running Your App

You're ready to go!

- Make sure Docker Desktop is running.
- Make sure PostgreSQL is running (`docker-compose up -d`).
- Build and run your app as usual:
  ```
  dotnet build
  dotnet run
  ```

If you see errors, check the FAQ below.

---

# Quick Recap

- You installed Git, .NET, and Docker Desktop.
- You started Docker and PostgreSQL using Docker Compose.
- You configured your .NET app to use your local PostgreSQL database.
- You are ready to build and run Jarvis ECS locally!

---

# FAQ

## Docker won't start or gives errors.
**A:** Make sure Docker Desktop is installed and running. Try restarting your computer after installation. If you installed from the Microsoft Store, launch Docker Desktop from your Start menu.

## `docker-compose up -d` fails or hangs.
**A:** Ensure Docker Desktop is running. If you see errors about ports, make sure nothing else is using port 5432 (like another PostgreSQL installation).

## I can't connect to PostgreSQL.
**A:** Make sure the container is running with `docker ps`. Check that you're using the correct connection details (localhost:5432, postgres/postgres).

## My .NET app can't connect to PostgreSQL.
**A:** Double-check your connection string. Make sure PostgreSQL is running (`docker ps` should show pg_graphql_db container).

## Where can I get more help?
- [Docker Desktop Troubleshooting Guide](https://docs.docker.com/desktop/troubleshoot/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Troubleshooting](../troubleshooting/troubleshooting-readme.md)
- Ask a teammate or mentor if you get stuck.

---

You now have a local Jarvis ECS project with PostgreSQL database ready for development.