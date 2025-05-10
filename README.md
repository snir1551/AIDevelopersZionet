# AI For Developers Zionet
<br />

<details>
<summary>Task 1 – GitPlugin Chatbot with Semantic Kernel</summary>
<br />

## 1. Develop a chatbot with ability to generate release notes using:

- Microsoft Semantic Kernel
- LibGit2Sharp
- GPT API



---

## 2. Features Implemented

- ✅ **SetRepository** – User can define the path to a local Git repository  
- ✅ **GetCommits** – Retrieves latest commit messages from the selected repository  
- ✅ **Generate Release Notes** – Uses a prompt-based plugin to generate structured release notes via LLM  
- ✅ **Set GitHub Credentials** – Secure runtime injection of GitHub username & token  
- ✅ **CommitChanges** – Stages and commits all changes with specified author info  
- ✅ **PullFromRemote / PushToRemote** – Supports syncing with GitHub remote (`origin`)  
- ✅ **SaveVersion / LoadVersion** – Persist and retrieve semantic version tags (stored locally)  

---


## 3. Semantic Kernel Integration

- Registered functions using `[KernelFunction]` attribute for natural language triggering  
- Included a **prompt plugin** (`skprompt.txt` + `config.json`) under `PromptPlugins/ReleasesNotes/`  
- Prompt template is modular, reusable, and includes:
  - Grouping of changes (Features, Bugfixes, Docs, etc.)
  - AI signature footer

---

## 4. Secure Configuration

Stored sensitive data like GitHub token and OpenAI keys in:

```json
appsettings.Development.json
{
  "ModelName": "",
  "Endpoint": "",
  "ApiKey": "",
  "GitHub": {
    "Token": "",
    "Username": ""
  }
}

```

</details>

******
