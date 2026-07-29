# GitDesktop

A lightweight Git GUI built from scratch using **C#**, **OpenGL**, **Silk.NET** and **Dear ImGui**.

GitDesktop was created with one goal in mind: **make Git simple for non-technical users while reducing the number of clicks required for everyday work.**

Instead of exposing every Git command, the application automates common workflows so users can focus on their work instead of version control.

<img width="1201" height="698" alt="gitDesktop" src="https://github.com/user-attachments/assets/34a56ca2-80d6-4ca9-b2cb-a65e220c0800" />

---

## Why GitDesktop?

Most Git GUI applications expose Git almost one-to-one, expecting users to understand concepts such as push, pull, fetch, merge and synchronization.

GitDesktop takes a different approach.

It automates repetitive operations and protects users from common mistakes, making Git much easier to use—especially for artists, designers, level designers and other team members who don't work with Git on a daily basis.

## Features

### 🚀 Automatic Push

After every successful commit, GitDesktop automatically pushes the changes to the remote repository.

There is no separate **Push** button and no additional step required.

---

### 🔄 Automatic Pull

Remote changes to current branch are detected automatically.

If new commits are available, the application synchronizes the repository without requiring manual fetch/pull operations.

---

### 📊 Progress window for large repositories

Updating from the main branch can take a while in large repositories.

GitDesktop displays a dedicated progress window so users always know what is happening instead of wondering whether the application has frozen.

---

### 🔒 Safe operation

While Git is performing an operation, the user interface becomes temporarily locked.

This prevents users from accidentally starting conflicting operations by clicking multiple buttons during an update.

This behaviour was inspired by real-world issues encountered in other Git GUI applications.

---

### 🔥 One-click Hard Reset

If something goes wrong, the current branch can be restored with a single click using **Hard Reset**.

No terminal required.

---

### 🌿 Update from Main

Keeping feature branches up to date is simplified into a single action.

The application handles the Git workflow internally while presenting only the progress to the user.

---

### 📂 Embedded Portable Git

GitDesktop ships with its own Portable Git distribution.

Users don't need to install Git manually before using the application.

Git LFS is also supported.

---

## Built With

- C#
- .NET
- OpenGL
- Silk.NET
- Dear ImGui
- Portable Git

---

## Project Goals

- Make Git accessible to non-technical users
- Reduce the number of required clicks
- Eliminate unnecessary Git concepts from the UI
- Prevent accidental user mistakes
- Provide a responsive experience even for very large repositories

---

## Status

GitDesktop is an actively developed personal project and new features are added continuously.
