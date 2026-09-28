-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================================
-- SHARED IDENTITY & AUTHENTICATION
-- ============================================================================
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('Admin', 'Organizer', 'Volunteer')),
    phone_number VARCHAR(20),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);


-- ============================================================================
-- STUDENT 1: EVENT MANAGEMENT & REQUIREMENTS
-- ============================================================================
CREATE TABLE venues (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL,
    address TEXT NOT NULL,
    city VARCHAR(100) NOT NULL,
    latitude NUMERIC(9, 6),
    longitude NUMERIC(9, 6),
    capacity INT NOT NULL CHECK (capacity > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organizer_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    venue_id UUID REFERENCES venues(id) ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,
    start_date TIMESTAMPTZ NOT NULL,
    end_date TIMESTAMPTZ NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft' 
        CHECK (status IN ('Draft', 'Published', 'StaffingInProgress', 'FullyStaffed', 'Completed', 'Cancelled')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_event_dates CHECK (start_date < end_date)
);

CREATE TABLE role_requirements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    required_headcount INT NOT NULL CHECK (required_headcount > 0),
    min_experience_level VARCHAR(20) DEFAULT 'Beginner' 
        CHECK (min_experience_level IN ('Beginner', 'Intermediate', 'Advanced', 'Expert')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_events_organizer ON events(organizer_id);
CREATE INDEX idx_events_status ON events(status);
CREATE INDEX idx_role_req_event ON role_requirements(event_id);


-- ============================================================================
-- STUDENT 2: VOLUNTEER & APPLICATION MANAGEMENT
-- ============================================================================
CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    category VARCHAR(50) NOT NULL,
    description TEXT
);

CREATE TABLE volunteer_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    emergency_contact VARCHAR(20) NOT NULL,
    bio TEXT,
    max_hours_per_week INT NOT NULL DEFAULT 20 CHECK (max_hours_per_week > 0),
    rating_score NUMERIC(3, 2) DEFAULT 5.00 CHECK (rating_score >= 0.0 AND rating_score <= 5.0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE volunteer_skills (
    volunteer_id UUID NOT NULL REFERENCES volunteer_profiles(id) ON DELETE CASCADE,
    skill_id UUID NOT NULL REFERENCES skills(id) ON DELETE RESTRICT,
    proficiency_level VARCHAR(20) DEFAULT 'Intermediate'
        CHECK (proficiency_level IN ('Beginner', 'Intermediate', 'Advanced')),
    PRIMARY KEY (volunteer_id, skill_id)
);

CREATE TABLE applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    volunteer_id UUID NOT NULL REFERENCES volunteer_profiles(id) ON DELETE CASCADE,
    role_requirement_id UUID REFERENCES role_requirements(id) ON DELETE SET NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Submitted'
        CHECK (status IN ('Submitted', 'UnderReview', 'Shortlisted', 'Accepted', 'Rejected', 'Assigned')),
    notes TEXT,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reviewed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_event_volunteer_app UNIQUE (event_id, volunteer_id)
);

CREATE INDEX idx_apps_event_status ON applications(event_id, status);
CREATE INDEX idx_apps_volunteer ON applications(volunteer_id);


-- ============================================================================
-- STUDENT 3: SHIFT SCHEDULING & ROSTERING
-- ============================================================================
CREATE TABLE shifts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    role_requirement_id UUID NOT NULL REFERENCES role_requirements(id) ON DELETE CASCADE,
    title VARCHAR(150) NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    capacity INT NOT NULL CHECK (capacity > 0),
    status VARCHAR(20) NOT NULL DEFAULT 'Scheduled' 
        CHECK (status IN ('Draft', 'Scheduled', 'InProgress', 'Completed', 'Cancelled')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_shift_times CHECK (start_time < end_time)
);

CREATE TABLE shift_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID NOT NULL REFERENCES shifts(id) ON DELETE CASCADE,
    volunteer_id UUID NOT NULL REFERENCES volunteer_profiles(id) ON DELETE CASCADE,
    status VARCHAR(20) NOT NULL DEFAULT 'Proposed_By_AI'
        CHECK (status IN ('Proposed_By_AI', 'Confirmed', 'Declined', 'Completed', 'Cancelled')),
    assigned_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_shift_volunteer UNIQUE (shift_id, volunteer_id)
);

CREATE TABLE shift_swap_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    requester_assignment_id UUID NOT NULL REFERENCES shift_assignments(id) ON DELETE CASCADE,
    target_volunteer_id UUID NOT NULL REFERENCES volunteer_profiles(id) ON DELETE RESTRICT,
    target_shift_id UUID NOT NULL REFERENCES shifts(id) ON DELETE CASCADE,
    reason TEXT,
    status VARCHAR(25) NOT NULL DEFAULT 'Pending_Target'
        CHECK (status IN ('Pending_Target', 'Pending_Organizer', 'Approved', 'Rejected', 'Cancelled')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_shifts_event ON shifts(event_id);
CREATE INDEX idx_assignments_volunteer ON shift_assignments(volunteer_id);
CREATE INDEX idx_assignments_shift ON shift_assignments(shift_id);


-- ============================================================================
-- STUDENT 4: ATTENDANCE, QR OPERATIONS & AGENTIC AI STATE
-- ============================================================================
CREATE TABLE qr_code_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id UUID NOT NULL REFERENCES shifts(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE attendance_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_assignment_id UUID NOT NULL UNIQUE REFERENCES shift_assignments(id) ON DELETE CASCADE,
    check_in_time TIMESTAMPTZ,
    check_out_time TIMESTAMPTZ,
    check_in_latitude NUMERIC(9, 6),
    check_in_longitude NUMERIC(9, 6),
    status VARCHAR(20) NOT NULL DEFAULT 'Pending'
        CHECK (status IN ('Pending', 'CheckedIn', 'CheckedOut', 'Absent', 'Excused')),
    verified_hours NUMERIC(5, 2) DEFAULT 0.0 CHECK (verified_hours >= 0.0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Persistent Storage for Agentic AI Workflows (Sections 6 & 9.1)
CREATE TABLE agent_workflow_runs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    initiated_by_user_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    status VARCHAR(30) NOT NULL DEFAULT 'Running'
        CHECK (status IN ('Running', 'AwaitingApproval', 'Approved', 'Rejected', 'Failed')),
    prompt_objective TEXT NOT NULL,
    plan_summary JSONB,                 -- Output from Planning Agent (Steps breakdown)
    generated_roster_proposal JSONB,    -- Output from Scheduling & Matching Agents
    validation_report JSONB,            -- Output from Validation Agent (Deterministic checks)
    reviewed_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    review_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Observability & Tool Tracing for Agentic AI (Section 9.1 & 12)
CREATE TABLE agent_tool_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_run_id UUID NOT NULL REFERENCES agent_workflow_runs(id) ON DELETE CASCADE,
    agent_name VARCHAR(50) NOT NULL,    -- 'PlanningAgent', 'MatchingAgent', 'SchedulingAgent', etc.
    tool_name VARCHAR(100) NOT NULL,    -- e.g. 'get_eligible_volunteers', 'assert_no_overlap'
    input_parameters JSONB,
    output_summary JSONB,
    execution_duration_ms INT NOT NULL,
    called_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_attendance_assignment ON attendance_records(shift_assignment_id);
CREATE INDEX idx_workflow_event_status ON agent_workflow_runs(event_id, status);
CREATE INDEX idx_tool_logs_workflow ON agent_tool_logs(workflow_run_id);