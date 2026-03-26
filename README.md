# ReqPipeline (USDM AI-Reviewer)

[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Ollama](https://img.shields.io/badge/Ollama-Local_LLM-black.svg)](https://ollama.ai/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**ReqPipeline** is a next-generation requirements specification editor and static analysis pipeline that fuses **USDM** (Universal Specification Descriptive Manner) with **Local LLMs**.

It semantically verifies logical contradictions in specifications that humans tend to overlook, detecting bugs in the ultra-upstream phase of development.

![ReqPipeline DEMO](docs/images/demo.gif)

## **Our Vision: "Empowering everyone to build the right thing, right"**

In many development environments, implementation proceeds while "ambiguity" remains in the requirements definition phase, leading to the tragedy of discovering context mismatches later on. ReqPipeline aims to eradicate the bug of "cognitive misalignment" in the earliest stages of development by combining powerful frameworks like USDM/EARS/BDD with an AI review system that instantly detects logical contradictions.

## **Motivation & Problem to Solve**

In software development, the **"Requirements Definition Phase" is where bugs are most easily introduced, yet it is also the phase where the cost of removing them is the lowest**.

If a contradiction in specifications is discovered during the implementation or testing phase, the cost of rework balloons to 10 to 100 times that of the requirements phase.

However, with traditional natural language requirements definition, it has been difficult to prevent "human-derived bugs" such as:

* **Context Mismatch**: Cognitive gaps due to "unspoken assumptions" between writers and developers.  
* **Ambiguity & Omission**: Missing specifications for abnormal systems and undefined edge cases.  
* **Logical Contradictions**: Silent conflicts between specifications when viewing the system as a whole.

**ReqPipeline is a tool to eradicate these "specification bugs" *before* writing code (Shift-Left).**

It physically prevents the ambiguities humans easily fall into through the strict "typing" of **USDM / EARS / BDD**, and an **AI pipeline** using a local LLM instantly detects logical contradictions that humans might miss, 24/7.

By removing bugs at the requirements definition stage, we realize a world where the entire team can focus on "building the right thing, right."

## **Getting Started**

ReqPipeline runs cross-platform (Windows, macOS, Linux).

The setup procedure to run the AI pipeline in a local environment is as follows:

### **1\. Prerequisites**

Ensure the following are installed in your execution environment:

* [**.NET 10.0 SDK**](https://dotnet.microsoft.com/download/dotnet/10.0)  
* [**Ollama**](https://ollama.ai/) (Local LLM execution engine)

### **2\. LLM Model Preparation**

This system uses the qwen2.5:7b model by default. Run the following command in your terminal to download the model.

*(Note: A download of several GBs will occur only the first time)*

ollama run qwen2.5:7b

**Note**: Please keep Ollama running in the background.

### **3\. Installation & Run**

Clone the repository and run the web project.

```bash
# Clone the repository  
git clone \[https://github.com/boxdate/ReqPipeline.git\](https://github.com/boxdate/ReqPipeline.git)  
cd ReqPipeline
```
```bash
\# Run the web application  
cd src/ReqPipeline.Web  
dotnet run
```

### **4\. Access via Browser**

After starting, access the URL displayed in the terminal (e.g., http://localhost:5000 or https://localhost:5001) with your browser.

## **Basic Usage**

### **Loading Sample Data**

The requirements.json (Requirements Specification Tree) and glossary.json (Glossary) located directly under the project (execution directory) are loaded automatically.

### **Editing Requirements, Rationales, and Specifications**

On the Web UI, add and edit "Requirements", "Rationales", and "Specifications" following the USDM format. You can set EARS contexts for the specifications.

### **Executing AI Validation**

Click the **"🔍 Run AI Validation"** button in the upper right corner of the screen.

The Orchestrator starts in the background, and the local LLM analyzes the specifications for contradictions and logical flaws.

### **Checking the Results**

If a contradiction is found, a **"⚠️ Contradiction"** badge and specific feedback (reason) from the AI will be displayed directly below the corresponding node. Correct the specification according to the feedback and run the validation again.

## **Customizing the LLM**

This system uses the lightweight qwen2.5:7b by default so that it can run on standard PCs. However, if you have an environment with ample VRAM, you can further improve the logical reasoning accuracy of the AI review by changing to a model with more parameters (e.g., qwen2.5:14b or qwen3.5:9b).

You can change the model using either of the following two methods:

#### **Method 1: Edit appsettings.json**

Open the src/ReqPipeline.Web/appsettings.json file and add/edit the following section:

```JSON
{  
  "OllamaSettings": {  
    "ModelName": "qwen2.5:14b"  
  }  
}
```

#### **Method 2: Use Environment Variables (Useful for CI/CD or temporary switches)**

You can also run it by specifying the environment variable OllamaSettings\_\_ModelName (two underscores).

*(For macOS / Linux)*
```bash
export OllamaSettings\_\_ModelName="qwen2.5:14b"  
dotnet run
```

*(For Windows PowerShell)*

```bash
$env:OllamaSettings\_\_ModelName="qwen2.5:14b"  
dotnet run
```

## **Contributing**

ReqPipeline is an open-source project aiming for a world where "everyone can write correct specifications."

We welcome contributions in various forms, not just writing code\!

### **Forms of Contribution We Welcome**

We look forward to the following contributions. Even if you can't write code, your "expertise" is the greatest contribution\!

* **Prompt Engineering with RE Expertise**  
  * Proposals for improving LLM prompts, such as the core SemanticValidator.cs.  
  * We are eager for PRs that incorporate **Requirements Engineering (RE) expertise** into prompts—like "Is it correct according to USDM conventions?", "Is the EARS syntax logical?", "Are any edge cases missed?"—to nurture the AI into an "expert requirements analyst"\! The knowledge of requirements development experts and QA engineers directly translates to the tool's intelligence.  
* **Bug Reports & Use Case Proposals (Issues)**  
  * Reports like "When I fed it an actual field specification, the AI missed this," or "This part of the UI is hard to use," as well as ideas for new features.  
* **Providing Sample Data for Validation**  
  * Sample cases of requirements.json containing typical contradictions or ambiguities found in the field, which can be used for testing.  
* **Code Contributions (Pull Requests)**  
  * Bug fixes, Blazor UI improvements, C\# architecture refactoring, etc.

### **Help Wanted\!**

In the future, we want to evolve this tool into an "infrastructure for team-wide collaboration." We are actively seeking engineers who are interested in or excel at the following areas\!

1. **Best Practices for Team Development using JSON files**  
   * Building and practicing best practices for developing requirements/specifications in a multi-person team using Git with local JSON files.  
2. **DB Design and Multi-User Support for Phase 2**  
   * Currently based on local JSON files, but we want to discuss and implement a backend design for team development using PostgreSQL, etc.  
3. **UI/UX Improvements (Blazor)**  
   * Implementing a UI where the USDM tree structure can be edited more intuitively with drag & drop.  
4. **Translation of Documents**  
   * Multilingual support to reach requirements engineers worldwide.

### **How to Start Developing**

1. Fork this repository.  
2. Create a new branch (git checkout \-b feature/amazing-feature).  
3. Commit your changes (git commit \-m 'Add some amazing feature').  
4. Push to the branch (git push origin feature/amazing-feature).  
5. Open a Pull Request\!

## **Tested Environments**

Because this system is based on .NET 10 and Ollama, it is designed to run cross-platform on Windows, macOS, and Linux.

However, currently, operation has only been verified in the author's local environment (Ubuntu / Linux).

**We greatly welcome feedback and Issue reports from those who have tried running it on Windows or Mac environments (especially Apple Silicon machines)\!**

Any small piece of information, like "It worked on Mac\!" or "I got an error with this path setting on Windows," is a huge help to the project.