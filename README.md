# HearthstoneAccess Beta Patcher
A script that automates patching Hearthstone using the [HearthstoneAccess beta Patch](https://www.hearthstoneaccess.com/)

## Overview
This script (referred to as the patcher) performs the following tasks:
* Downloads the zipped archive of the HearthstoneAccess Beta patch
* Unzips the archive
* Places the contents in your Hearthstone installation directory
* Prompts if you want the changelog put on your desktop
* Removes leftover/temporary files and directories

There are two general target audiences: those who wish to simply use the patcher, and those who wish to develop/build the patcher.

## Using the Patch
To use the patch, download the latest executable from the [latest release page](https://github.com/leibylucw/hearthstone-access-patcher/releases/latest) and run like any other program.

## Developing the Patcher
### Install System Dependencies
Make sure you have the following dependencies installed and available on your system:
* Git: any recent version
* [gh (the GitHub CLI)](https://cli.github.com/) (optional): any recent version
* [CPython](https://www.python.org/): version 3.11 or higher is supported

### Clone the Repo
With Git:

```shell
git clone https://github.com/leibylucw/hearthstone-access-patcher.git
cd hearthstone-access-patcher
```

Or with gh:

```shell
gh repo clone leibylucw/hearthstone-access-patcher
cd hearthstone-access-patcher
```

### Create a Virtual Environment (virtualenv)
A virtual environment (virtualenv) must be used when developing or consuming the package. All subsequent sections assume that you have created the virtualenv and have activated it. To create it, run the following command:

```sh
python -m virtualenv .venv
```

### Activate the Virtualenv
For Command Prompt on Windows:

```batch
.\.virtualenv\Scripts\activate.bat
```

For PowerShell on Windows:

```powershell
.\.virtualenv\Scripts\activate.ps1
```

For Linux/MacOS:

```sh
./.virtualenv/Scripts/activate
```

To deactivate the virtualenv on all platforms:

```shell
deactivate
```

### Install Project Requirements
The patcher requires certain dependencies to be installed. To install them, run the following commands with the virtualenv activated:

```shell
python -m pip install --upgrade pip
pip install -r requirements.txt
```

### Running the Patcher
Finally, run the patcher with Python:

```shell
python hearthstone-access-patcher.py
```

### General Development Notes
You will notice from the various configuration files in the repo that there are several tools to ensure certain code hygiene and quality conventions are enforced. You may wish to become familiar with these tools and the coding style configurations therein. For more info, please refer to:
* (Ruff)[https://github.com/astral-sh/ruff]: used for code linting and formatting
* (pre-commit](https://github.com/pre-commit/pre-commit): used for managing pre-commit hooks

### Building the Patcher
The patcher uses Pyinstaller to create a Windows executable. It was installed as part of the requirements in the development instructions. To build to a Windows binary (`.exe`), run the following command:

```
pyinstaller --onefile hearthstoneAccessPatcher.py
```

Check the `dist` directory for the resulting executable.
