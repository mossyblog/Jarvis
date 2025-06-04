# Setting Up Your .NET Environment

Welcome! Let's get your computer ready for Jarvis ECS. We'll go step by step. If you get stuck, don't worry—there's help at the end of every section.

## What is .NET?
.NET is a framework for building apps. Jarvis ECS is built with .NET, so you need it installed.

## What you need:
- **Git**: Lets you download code from the internet.
- **.NET SDK 8.0 or later**: Lets you build and run .NET apps.
- **Node.js & npm**: Lets you install tools like Supabase CLI.

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

3. **Install Node.js & npm**
   - Go to [https://nodejs.org/en/download/](https://nodejs.org/en/download/)
   - Click the big green **LTS** button (recommended for most users).
   - Open the file you downloaded. Click **Next** a lot, then **Install**.
   - When it finishes, click **Finish**.
   - To check it's installed: In **Command Prompt**, type:
     ```
     node --version
     npm --version
     ```
     You should see two numbers, like `v20.0.0` and `10.0.0`.

4. **Clone the Jarvis project**
   - In **Command Prompt**, go to the folder where you want the code (e.g., `C:\Code`):
     ```
     cd C:\Code
     ```
   - Run:
     ```
     git clone <your-jarvis-repo-url>
     cd core.jarvis
     ```
   - You should see a bunch of folders and files if it worked.

---

# Installing Scoop (Optional, but Recommended)

Scoop is a package manager for Windows. It's like Homebrew for Mac. Scoop makes it super easy to install and update developer tools (like git, nodejs, supabase, docker, and more) with just a single command.

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
- To install nodejs:
  ```
  scoop install nodejs-lts
  ```
- To install supabase:
  ```
  scoop bucket add extras
  scoop install supabase
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

# Setting Up Docker

Docker lets you run software in a "container"—like a mini-computer inside your computer. Jarvis ECS uses Docker to run Supabase, so you don't have to install a database yourself.

## What is Docker?
- It's a tool for running apps in a safe, isolated way.
- You need it for Supabase to work locally.

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

# Installing the Supabase CLI

The Supabase CLI is a tool you use in the terminal to run Supabase on your computer. It's like a remote control for your local database.

## What is the Supabase CLI?
- It's a command-line tool (you type commands, not click buttons).
- It helps you start, stop, and manage your local Supabase.

## Step-by-step:

1. **Install the CLI**
   - Open **Command Prompt**.
   - Type:
     ```
     npm install -g supabase
     ```
   - Wait for it to finish. You should see a bunch of lines ending with `+ supabase@...`.
   - If you get a permissions error, close the terminal, right-click and choose **Run as administrator**, then try again.

2. **Check the CLI is installed**
   - In **Command Prompt**, type:
     ```
     supabase --version
     ```
   - You should see something like `supabase 1.123.4`.

3. **Update the CLI (Optional)**
   - To get the latest version later, type:
     ```
     npm update -g supabase
     ```

4. **Uninstall the CLI (Optional)**
   - If you ever want to remove it, type:
     ```
     npm uninstall -g supabase
     ```

5. **Troubleshooting Supabase CLI**
   - If `supabase` is not recognized, close and reopen your terminal, or restart your computer.
   - Make sure Node.js and npm are installed and available in your PATH.
   - If you have multiple versions of Node.js, make sure you're using the one where you installed the CLI.
   - For more help, see the [Supabase CLI documentation](https://supabase.com/docs/guides/local-development/cli/getting-started?queryGroups=platform&platform=windows).



---

# Running Supabase Locally

Now you'll start your own Supabase database on your computer. This is where Jarvis ECS will store its data.

## Step-by-step:

1. **Initialize a local Supabase project**
   - In **Command Prompt**, make sure you're in your project folder (e.g., `C:\Code\core.jarvis`).
   - Type:
     ```
     supabase init
     ```
   - This creates a new `supabase/` folder in your project. You only need to do this once per project.

2. **Start Supabase**
   - In the same folder, type:
     ```
     supabase start
     ```
   - The first time, this will download a lot of files (Docker images). It may take several minutes.
   - When it's done, you'll see output like:
    ```
    Started supabase local development setup.
    API URL: http://127.0.0.1:54321
    GraphQL URL: http://127.0.0.1:54321/graphql/v1
    S3 Storage URL: http://127.0.0.1:54321/storage/v1/s3
    DB URL: postgresql://postgres:postgres@127.0.0.1:54322/postgres
    Studio URL: http://127.0.0.1:54323
    Inbucket URL: http://127.0.0.1:54324
    JWT secret: super-secret-jwt-token-with-at-least-32-characters-long
    anon key: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0
    service_role key: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImV4cCI6MTk4MzgxMjk5Nn0.EGIM96RAZx35lJzdJsyH-qQwv8Hdp7fsn3W0YpN81IU
    S3 Access Key: 625729a08b95bf1b7ff351a663f3a23c
    S3 Secret Key: 850181e4652dd023b7a98c58ae0d2d34bd487ee0cc3254aed6eda37307425907
    S3 Region: local
     ```
   - **Leave this terminal open** while you work. Supabase must be running for your app to connect.

3. **If you need to stop Supabase**
   - In the terminal, type:
     ```
     supabase stop
     ```
   - This will stop all the Supabase services. You can start them again with `supabase start`.

4. **If you want to reset your local database**
   - This will delete all your data! Only do this if you want a fresh start.
   - Type:
     ```
     supabase stop --no-backup
     ```

---

**What to do if you get stuck:**
- If you see errors about Docker, make sure Docker Desktop is running.
- If you see errors about ports, make sure nothing else is using ports 54321-54324.
- If you get a permissions error, try running the terminal as Administrator.
- For more help, see the [Supabase Local Development Guide](https://supabase.com/docs/guides/local-development/cli/getting-started?queryGroups=platform&platform=windows).

---

# Using Supabase Studio

Supabase Studio is a web interface (like a website) for managing your local database. It's much easier than typing SQL by hand!

## Step-by-step:

1. **Open Supabase Studio**
   - Open your web browser (Chrome, Edge, Firefox, etc.).
   - Go to [http://localhost:54323](http://localhost:54323)
   - You should see the Supabase Studio dashboard.

2. **Open the SQL Editor**
   - In the left menu, click **SQL Editor** (it looks like a little database with a pencil).
   - Click **New Query** (top right).

3. **Run the setup script**
   - Open the file `core.jarvis.tests/Scripts/setup-test-database.sql` in a text editor (like Notepad).
   - Select all the text (Ctrl+A), then copy it (Ctrl+C).
   - Go back to Supabase Studio and paste it into the SQL Editor (Ctrl+V).
   - Click **Run** (top right).
   - You should see a message like `Test database setup complete!`.

4. **If you see errors**
   - Double-check you copied the whole script.
   - Make sure Supabase is running (see previous section).
   - Try refreshing the page and running the script again.

---

**What to do if you get stuck:**
- Check that Supabase is running (`supabase start` in your terminal).
- Make sure Docker Desktop is running.
- If you see a "connection refused" error, try restarting Docker and Supabase.
- Ask for help if you need it—everyone gets stuck sometimes!

---

# Configuring Your .NET Application

Now you'll tell your .NET app how to connect to your local Supabase database.

## Step-by-step:

1. **Find your Supabase keys**
   - In the terminal where you ran `supabase start`, look for lines like:
     - `API URL: http://localhost:54321`
     - `service_role key: ...`
   - Copy these values—you'll need them in your code.

2. **Update your .NET app**
   - In your app's startup code, add:
     ```csharp
     services.AddJarvisECS(options =>
     {
         options.UseSupabaseStorage(config =>
         {
             config.Url = "http://localhost:54321"; // Local Supabase API URL
             config.Key = "<your-service_role-key>"; // Copy from supabase start output
         });
     });
     ```
   - Replace `<your-service_role-key>` with the actual key from your terminal.

3. **Save your changes**
   - Make sure to save the file before running your app.

---

# Running Your App

You're ready to go!

- Make sure Docker Desktop and Supabase are running.
- Use the local API URL and service_role key in your app config.
- Build and run your app as usual.

If you see errors, check the FAQ below.

---

# Quick Recap

- You installed Git, .NET, Node.js, Docker Desktop, and Supabase CLI.
- You started Docker and Supabase.
- You set up your local database with Supabase Studio.
- You configured your .NET app to use your local database.
- You are ready to build and run Jarvis ECS locally!

---

# FAQ

## Docker won't start or gives errors.
**A:** Make sure Docker Desktop is installed and running. Try restarting your computer after installation. If you installed from the Microsoft Store, launch Docker Desktop from your Start menu.

## `npm install -g supabase` fails with permissions errors.
**A:** Try running your terminal as Administrator.

## `supabase start` fails or hangs.
**A:** Ensure Docker Desktop is running. If you see errors about ports, make sure nothing else is using ports 54321-54324.

## I can't access Supabase Studio at http://localhost:54323
**A:** Make sure `supabase start` is running and Docker is running. Try refreshing your browser or restarting Supabase.

## My .NET app can't connect to Supabase.
**A:** Double-check your `config.Url` and `config.Key` values. They must match the output from `supabase start`. Make sure Supabase is running.

## Where can I get more help?
- [Docker Desktop Troubleshooting Guide](https://docs.docker.com/desktop/troubleshoot/)
- [Supabase Local Development Guide](https://supabase.com/docs/guides/local-development/cli/getting-started?queryGroups=platform&platform=windows)
- [Troubleshooting](../troubleshooting/troubleshooting-readme.md)
- Ask a teammate or mentor if you get stuck.

---

You now have a local Jarvis ECS project and Supabase instance ready for development. 