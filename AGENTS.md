# AGENTS.md

## Project
- Unity project
- C#
- Photon PUN2 multiplayer project

## Environment
- Unity version: 6000.2.10f1
- Goal: preserve the current project structure

## Rules
- Prefer minimal and safe edits
- Do not rename serialized fields unless absolutely necessary
- Do not change network authority flow without explaining the reason first
- After any change, explain which scripts were modified and why
- Preserve existing public APIs unless there is a clear reason to change them

## Do Not Modify
- Scene or prefab references unless required
- Core multiplayer flow without a clear reason
- Inspector-linked fields unless necessary

## Validation
- Point out any possible Unity Inspector relinking issues
- Mention side effects that may affect multiplayer behavior

## Working Style
- Analyze first before making code changes
- Ask for broad refactoring only when necessary