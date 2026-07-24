# CMS

A minimal ASP.NET Core MVC starter (.NET 10, current LTS) with three demo buttons on the home page. Deploys to an Ubuntu EC2 instance behind Nginx, built and shipped via an Azure DevOps pipeline. Structured so a separate REST API project can be added later without restructuring anything.

## Project layout

```
cms-app/
├── src/
│   └── CMS/                  # the MVC web app
├── deploy/
│   ├── webapp.service        # systemd unit that runs Kestrel
│   └── nginx-webapp.conf     # Nginx reverse proxy config
├── azure-pipelines.yml       # ADO build + deploy pipeline
├── CMS.sln
└── .gitignore
```

## 1. Local development on your Mac

Install the .NET 10 SDK:

```bash
brew install --cask dotnet-sdk
dotnet --version   # should print 10.x
```

Open the folder in VS Code and install the **C# Dev Kit** extension (search the Extensions panel) — it gives you IntelliSense, debugging, and a "Run" button for the project.

Run it:

```bash
cd src/CMS
dotnet run
```

Open `https://localhost:5001` (or the URL printed in the terminal). Click the three buttons on the home page — one is pure client-side JS, one keeps state in the browser, and one calls back into the MVC controller via `fetch()`. That third pattern is deliberately the one you'll reuse once the REST API exists: same `fetch()` call, just pointed at the API's URL instead of `/Home/Ping`.

## 2. Git and GitHub

```bash
cd cms-app
git init
git add .
git commit -m "Initial CMS scaffold"
gh repo create your-org/cms --private --source=. --push
```

(Or create the repo on github.com first and `git remote add origin ...` + `git push -u origin main`.)

## 3. Azure DevOps pipeline

ADO can build from a GitHub-hosted repo directly:

1. In your ADO project, go to **Pipelines > New pipeline > GitHub**, authorize access, and select this repo. Point it at `azure-pipelines.yml` at the repo root.
2. Before the first run, do two one-time setup steps referenced in the pipeline:
   - **Pipelines > Library > Secure files**: upload your EC2 SSH private key (the `.pem` you'll create in step 4) and name it to match `sshKeySecureFile` in the YAML.
   - **Pipelines > Environments**: create an environment named `production` and add an approval check on it, so the deploy stage pauses for a manual sign-off before touching EC2 — the same pattern from the Azure DevOps interview prep doc, if you want a refresher on how environment checks work.
3. Update the placeholder variables at the top of `azure-pipelines.yml` (`ec2Host`, `ec2User`, `deployPath`) once your instance exists.

The pipeline has two stages: **Build** (restore/build/test/publish, uploads the published output as a pipeline artifact) and **Deploy** (downloads that artifact, `scp`s it to the EC2 instance over SSH, restarts the `webapp` systemd service). The deploy stage only runs after the `production` environment's approval is granted.

## 4. AWS EC2 setup (one-time, per environment)

1. Launch an Ubuntu 22.04/24.04 EC2 instance. Open inbound ports 22 (SSH, ideally restricted to your IP), 80, and 443 in its security group.
2. Generate or reuse an SSH key pair; keep the `.pem` private key for the ADO secure file upload in step 3 above. Never commit it to the repo — it's already covered by `.gitignore`.
3. SSH in and install the .NET **runtime** (the EC2 box only needs to run the app, not build it — the SDK isn't required here):

   ```bash
   sudo apt-get update
   sudo apt-get install -y aspnetcore-runtime-10.0
   ```

   (If that package isn't in the default Ubuntu repos yet, follow Microsoft's install docs for adding the `packages.microsoft.com` apt feed first.)

4. Install Nginx:

   ```bash
   sudo apt-get install -y nginx
   ```

5. Copy `deploy/nginx-webapp.conf` and `deploy/webapp.service` onto the instance and install them — each file has the exact commands in its header comments. In short: drop the `.conf` into `/etc/nginx/sites-available/`, symlink it into `sites-enabled`, reload Nginx; drop the `.service` into `/etc/systemd/system/`, `daemon-reload`, `enable`, `start`.
6. Create the deploy directory the pipeline writes to: `sudo mkdir -p /var/www/webapp && sudo chown ubuntu /var/www/webapp` (matches `deployPath` in the pipeline).
7. Push to `main` — the pipeline builds, waits for your approval on the `production` environment, then deploys and restarts the service. Visit the instance's public DNS name in a browser.

Once you're ready for a real domain and TLS, `nginx-webapp.conf` has a commented-out HTTPS server block to adapt — pair it with `certbot` for a free Let's Encrypt certificate.

## 5. Adding the REST API later

When you're ready, add a sibling project rather than growing this one:

```bash
cd src
dotnet new webapi -n CMS.Api
cd ..
dotnet sln add src/CMS.Api/CMS.Api.csproj
```

Two ways to expose it, in order of how much this scaffold already anticipates:

- **Same box, different port, proxied by path**: run the API alongside the web app (its own systemd service + port, e.g. `5100`), and add a second `location /api/ { proxy_pass http://127.0.0.1:5100; }` block to `nginx-webapp.conf`. The web app calls `/api/...` on its own origin, so there's no CORS to configure. `appsettings.json` already has an `Api:BaseUrl` placeholder for this.
- **Separate deployment entirely** (its own EC2 instance, or later an ECS/EKS service): the web app calls the API's own domain directly via `HttpClient`, and you'll need to enable CORS on the API for the web app's origin.

Either way, the `Ping` button pattern in `Views/Home/Index.cshtml` and the commented-out `AddHttpClient` block in `Program.cs` are the two places you'll touch first.
