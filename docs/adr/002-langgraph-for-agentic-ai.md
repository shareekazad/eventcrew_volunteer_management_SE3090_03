# ADR-002: LangGraph for Agentic AI Orchestration

- **Date:** 2026-09-23
- **Status:** Accepted
- **Deciders:** The Team (IT24104324, IT24102554, IT24102868, IT24103013)

## Context

The Agentic AI subsystem must:
- Receive a domain objective (plan staffing for an event)
- Execute a **structured multi-step plan** with distinct agent roles
- Call **allow-listed tools** with validated inputs and structured outputs
- Persist workflow state (workflow ID, plan, tool results, errors, approvals)
- Apply **deterministic validation** (schema/business rules)
- Support **safe failure** — a failed step must not silently corrupt downstream state
- Support **human approval** for high-impact actions
- Provide an **auditable execution summary** (tool calls, timings, decisions)

Per SE3090 Section 9.1, we need at least four distinct agents with controlled tool permissions. Each agent must have an identifiable responsibility, defined input/output contract, and visible participation in the workflow.

We need a framework that:
- Is Python-first (the AI ecosystem lives in Python)
- Supports **multi-agent orchestration** declaratively
- Handles async tools cleanly
- Has a clear mental model for junior engineers on the team
- Is maintainable over a 9-week timeline

## Options Considered

### 1. Custom orchestration (plain Python)
- ✅ No framework learning curve
- ✅ Complete control
- ❌ We reinvent state passing, retries, conditional routing, and observability
- ❌ Harder to demonstrate formal agent workflows for evaluation
- ❌ Becomes unmaintainable as we add agents

### 2. LangChain Agents
- ✅ Rich ecosystem of tools + integrations
- ❌ Higher-level abstraction can obscure what's actually happening
- ❌ Historically less predictable in production (agent loops can misbehave)
- ❌ Many features we don't need

### 3. Microsoft AutoGen
- ✅ Strong multi-agent conversation model
- ❌ Heavier setup; conversation-first paradigm less natural for tool-driven workflows
- ❌ Steeper learning curve for the team
- ❌ More oriented toward chat-style interactions than deterministic pipelines

### 4. LangGraph
- ✅ **State-graph model** matches our mental picture: nodes = steps, edges = transitions, state = shared dict
- ✅ **Conditional edges** give us safe-failure routing out-of-the-box
- ✅ **Checkpointing** available for workflow persistence (aligned with our DB state)
- ✅ **Lab-tested** — LangGraph was used in SE3090 lab exercises, so team familiarity is higher
- ✅ Async-native; works cleanly with our HTTP tools
- ✅ No hidden magic — we see each node's input/output
- ⚠️ Younger framework; API has evolved across versions
- ⚠️ Some documentation is still maturing

## Decision

**Use LangGraph as the orchestration layer for the Agentic AI subsystem.**

**Architectural pattern:**

- **Tools** are plain async Python functions (`get_event`, `get_venue`, `calculate_staffing_ratio`)
- **Nodes** are small functions that call tools and update state
- **State** is a Pydantic `PlanningState` model
- **Graph** wires nodes with edges; **conditional edges** short-circuit to `END` on `state.status == "failed"`
- **FastAPI** exposes the graph via `/agent/plan` (HTTP boundary)
- **ASP.NET Core** is the only client of the Python service
- **PostgreSQL** persists workflow runs and tool logs

The graph itself is version-controlled, testable in isolation (with mocked tools), and documented through this ADR plus inline code comments.

## Consequences

**Positive:**
- Declarative multi-agent workflow — visible to evaluators in code structure
- Conditional edges give us **safe failure** without custom error plumbing
- Each node is unit-testable (15 pytest tests exercise nodes + graph)
- Future agents (Matching, Scheduling, Validation) slot in as new nodes + edges
- Aligns with SE3090 lab experience — lower knowledge risk

**Negative:**
- Tied to LangGraph's API and lifecycle (mitigated by keeping node functions framework-agnostic)
- Slightly more boilerplate than a monolithic Python function (mitigated by node reuse)

**Neutral:**
- If we later need to swap to a different orchestrator (e.g., Microsoft Agent Framework), the node functions and tools remain intact — only the graph wiring changes
- Checkpointing is available but not yet used; we currently persist to PostgreSQL from the ASP.NET Core side

## References

- LangGraph documentation: https://langchain-ai.github.io/langgraph/
- SE3090 lab material on agent frameworks
- Implementation: `ai-service/app/graphs/`
- Tests: `ai-service/tests/test_planning_graph.py`