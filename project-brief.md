# Tactical Twin — High-Level Project Brief

> Working concept: an immersive, site-specific training platform that recreates real indoor environments so security personnel can rehearse navigation, observation, communication, and decision-making in a realistic digital setting.

## 1. Overview

Tactical Twin is a VR-inspired spatial simulation platform for desktop and laptop use, with optional future VR support. The system captures a real location (for example, a bank lobby, entry corridor, teller area, or back office), reconstructs it into a digital scene, and allows personnel to train inside that familiar environment.

The core value is not arcade-style gameplay. The core value is **site familiarity under pressure**: helping personnel build confidence in navigation, orientation, line-of-sight awareness, movement through connected rooms, communication, and scenario recognition inside places they actually work.

## 2. Problem Statement

Security personnel often train in generic ranges, classrooms, or abstract simulations that do not match the physical layouts they protect every day.

This creates a gap between:
- theoretical training and real-world environment familiarity,
- static instruction and dynamic scenario rehearsal,
- range skills and spatial decision-making inside actual sites.

The project aims to close that gap by turning real operational spaces into immersive digital training environments.

## 3. Vision

Create a platform where a real location can be captured quickly and transformed into a navigable simulation that supports repeatable, scenario-based training.

Long term, the platform should enable:
- site-specific rehearsal,
- spatial awareness training,
- incident-response drills,
- communication and coordination exercises,
- measurable performance review,
- rapid content updates when layouts change.

## 4. Primary Users

### Direct users
- Security guards
- Armed or unarmed site personnel
- Supervisors and training leads
- Private security organizations

### Secondary users
- Risk managers
- Site operators
- Training designers
- Compliance and audit stakeholders

## 5. Goals

### Product goals
- Recreate real indoor sites with enough realism to feel familiar.
- Allow desktop-first navigation with mouse and keyboard.
- Support scenario-based training in one or more connected rooms.
- Provide repeatable drills with measurable outcomes.
- Make scenario authoring easier than building a full game level from scratch.

### Training goals
- Improve spatial familiarity with known environments.
- Improve route recall and entry/exit awareness.
- Improve observation and recognition of environmental details.
- Improve communication and decision-making under time pressure.
- Enable structured after-action review.

## 6. Non-Goals

At the initial stage, the product should **not** aim to be:
- a full military simulator,
- a physics-perfect ballistics engine,
- a generalized open-world FPS,
- a replacement for legal, compliance, or live safety training,
- a system for offensive tactics development.

The first versions should focus on **site realism, immersion, scenario logic, and evaluation**.

## 7. Core Use Cases

### Use Case A — Site Familiarization
A guard explores a digital copy of a real location to learn layout, transitions, visibility, blind spots, and key environmental reference points.

### Use Case B — Scenario Rehearsal
A trainee runs scripted or semi-randomized security scenarios inside a known environment, practicing recognition, movement, communication, and response sequencing.

### Use Case C — Supervisor Review
A trainer observes or replays the session, reviews decisions, compares paths and timing, and identifies gaps in environmental awareness.

### Use Case D — Layout Change Readiness
When the real site changes, the digital twin is updated and personnel can rehearse in the new layout before or shortly after deployment.

## 8. High-Level Product Concept

The product has four major layers:

1. **Capture Layer**  
   Record the real environment using phone video, photos, LiDAR-enabled device capture, or specialized scanning tools.

2. **Reconstruction Layer**  
   Convert captured media into a navigable 3D scene using room scanning, photogrammetry, Gaussian splatting, or similar reconstruction workflows.

3. **Simulation Layer**  
   Add navigation, scenario triggers, branching events, interactive points, role logic, and assessment systems.

4. **Training & Review Layer**  
   Track session data, replay runs, score objectives, compare attempts, and generate after-action summaries.

## 9. Experience Principles

The simulation should feel:
- **familiar** — the site should be recognizable to personnel,
- **grounded** — the environment should look and behave plausibly,
- **repeatable** — scenarios should be easy to rerun,
- **measurable** — outcomes should be reviewable,
- **accessible** — the first usable version should run on a normal laptop.

## 10. Phase Plan

## Phase 1 — Simple Proof of Concept

**Goal:** Prove that a real room can become a believable, navigable training scene.

### Scope
- Capture one room or two connected rooms.
- Create a navigable desktop simulation.
- Validate that the environment feels recognizable.
- Test basic scenario triggers and event states.

### Candidate approaches
- Polycam or similar room-capture workflow for fast testing
- Gaussian splat / image-based reconstruction for more realism
- Web or lightweight engine-based viewer for desktop navigation

### Success criteria
- Users recognize the site immediately.
- Users can navigate it smoothly with mouse and keyboard.
- A simple scripted scenario can be completed end-to-end.

## Phase 2 — Training MVP

**Goal:** Move from scene demo to structured training tool.

### Scope
- Multiple rooms or a small full site
- Scenario scripting
- Session state and checkpoints
- Basic analytics and replay
- Trainer dashboard or review mode

### MVP feature examples
- Spawn positions
- Trigger zones
- Timed events
- Role states
- Branching scenarios
- Session logging
- Score or assessment summaries

## Phase 3 — Advanced Simulation Platform

**Goal:** Support broader deployment and richer training workflows.

### Scope
- Larger sites
- Multi-user or instructor-led sessions
- Role-based scenarios
- Better content pipeline
- Stronger analytics
- Optional VR headset support
- Integration with training administration workflows

## 11. Core Modules

### 11.1 Environment Capture
Responsibilities:
- record rooms and connected spaces,
- preserve enough detail for orientation and recognition,
- support repeat capture when the site changes.

Questions:
- What capture method gives the best tradeoff between speed and realism?
- How often will real sites need rescanning?
- What minimum hardware should field teams need?

### 11.2 Scene Reconstruction
Responsibilities:
- generate an explorable digital environment,
- keep scale and layout credible,
- optimize for laptop performance.

Questions:
- Should the first implementation use room scans, photogrammetry, or Gaussian splats?
- How much cleanup is required before a scene is usable?
- What output format best fits the simulation engine?

### 11.3 Simulation Runtime
Responsibilities:
- navigation,
- camera control,
- interaction system,
- event logic,
- scenario state management.

Questions:
- Web-based runtime or game engine?
- Desktop-first only, or VR-ready from the beginning?
- How much realism is needed for the first version?

### 11.4 Scenario Authoring
Responsibilities:
- define starting states,
- configure events and roles,
- place triggers,
- set learning objectives,
- support easy iteration.

Questions:
- Can non-developers author scenarios?
- What should be editable in JSON, YAML, or a visual editor?
- How should scenario templates be stored and versioned?

### 11.5 Analytics and Review
Responsibilities:
- session logging,
- timing and route analysis,
- event timeline review,
- replay and trainer feedback.

Questions:
- What metrics matter most in early testing?
- What data is useful without becoming too complex?
- How should after-action review be presented?

## 12. Sample Training Flow

1. Select site
2. Select scenario
3. Load the digital environment
4. Brief the trainee
5. Run the scenario
6. Record actions, timing, route, and outcomes
7. Replay and review the run
8. Repeat with changed conditions

## 13. Example Scenario Categories

These should remain high level at the planning stage:
- orientation and route recall,
- entry and exit awareness,
- environmental observation,
- suspicious activity recognition,
- communication under stress,
- coordination between personnel,
- escalation/de-escalation decision points,
- post-incident review and learning.

## 14. Technical Direction (Initial)

### Frontend / runtime options
- Web-based 3D viewer for easy sharing and fast iteration
- Game-engine-based runtime for richer simulation later

### Reconstruction options
- Fast capture tools for POC
- Open-source reconstruction stack for custom pipeline later
- Gaussian splatting or photogrammetry for realism testing

### Data assets
- Capture media
- Scene outputs
- Scenario definitions
- Interaction maps
- Session logs
- Review artifacts

## 15. Recommended Build Strategy

### Track A — Fast Validation
Use existing capture/reconstruction tools to validate the idea quickly.

Purpose:
- test realism,
- test user reaction,
- test whether known sites produce training value.

### Track B — Custom Product Path
Once value is validated, build a custom simulation layer and content pipeline.

Purpose:
- own the workflow,
- reduce dependency on third-party tools,
- tailor authoring, runtime, and analytics to the training use case.

## 16. Key Risks

### Product risks
- reconstructed scenes may look impressive but feel difficult to navigate,
- scene quality may vary widely by lighting and capture quality,
- performance may suffer on normal laptops,
- scenario creation may become too manual.

### Operational risks
- real sites may contain sensitive security details,
- captured data may require access controls and retention policies,
- layout updates may create ongoing maintenance overhead,
- organizations may need clear separation between training realism and unsafe overreach.

## 17. Safety, Privacy, and Governance

This project should be designed with strong safeguards from the start.

Requirements:
- role-based access to site scans and simulations,
- protected storage for captured environments,
- audit trail for scenario changes,
- clear usage boundaries,
- compliance review before deployment,
- support for scenario content that emphasizes judgment, communication, and lawful response.

## 18. Success Metrics

Early metrics:
- time to capture and publish a usable site,
- user-rated realism and recognizability,
- navigation smoothness on target hardware,
- scenario completion reliability,
- trainer satisfaction with replay/review.

Later metrics:
- reduced familiarization time for new personnel,
- improved consistency in scenario performance,
- improved retention of site layout knowledge,
- reduced friction in recurring training cycles.

## 19. Open Questions

- What is the minimum believable quality for training value?
- Should the first product be desktop-only?
- What is the best capture standard for repeatability?
- What scenario-authoring workflow is sustainable?
- Which review metrics matter most to supervisors?
- What level of realism is necessary before it becomes operationally useful?

## 20. Suggested Initial Repo Structure

```text
/docs
  project-brief.md
  product-requirements.md
  architecture-notes.md
  scenario-design.md

/research
  capture-options.md
  reconstruction-options.md
  runtime-options.md

/data-models
  scenario-schema.md
  site-schema.md
  session-log-schema.md

/prototypes
  poc-web-viewer/
  poc-scene-pipeline/

/assets
  reference-images/
  sample-scenes/
```

## 21. Suggested Next Documents

After this brief, the next useful files are:
- `product-requirements.md`
- `poc-plan.md`
- `capture-pipeline-options.md`
- `simulation-runtime-options.md`
- `scenario-schema.md`
- `risk-and-governance.md`

## 22. One-Sentence Summary

Tactical Twin is a site-specific immersive simulation platform that turns real indoor environments into repeatable digital training spaces for spatial awareness, scenario rehearsal, and structured performance review.
