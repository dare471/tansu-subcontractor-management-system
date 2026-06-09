-- Tansu subcontractors — initial schema. Idempotent: safe to re-run.

CREATE SCHEMA IF NOT EXISTS subcontract;

SET search_path TO subcontract, public;

CREATE TABLE IF NOT EXISTS subcontract.project_refs (
    project_oid uuid PRIMARY KEY,
    name        text,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS subcontract.subcontractors (
    id          uuid PRIMARY KEY,
    name        varchar(500) NOT NULL,
    bin         varchar(32)  NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT  uq_subcontractors_bin UNIQUE (bin)
);

CREATE TABLE IF NOT EXISTS subcontract.project_subcontractors (
    project_oid       uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    subcontractor_id  uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    created_at        timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (project_oid, subcontractor_id)
);

CREATE TABLE IF NOT EXISTS subcontract.users (
    id                    uuid PRIMARY KEY,
    full_name             varchar(500) NOT NULL,
    position              varchar(300) NOT NULL,
    email                 varchar(320) NOT NULL,
    password_hash         text,
    user_type             varchar(32)  NOT NULL,
    subcontractor_id      uuid REFERENCES subcontract.subcontractors(id) ON DELETE RESTRICT,
    must_change_password  boolean NOT NULL DEFAULT false,
    is_active             boolean NOT NULL DEFAULT true,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_users_user_type CHECK (user_type IN ('TANSU','Subcontractor'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_email_lower
    ON subcontract.users (LOWER(email));

CREATE TABLE IF NOT EXISTS subcontract.employees (
    id               uuid PRIMARY KEY,
    subcontractor_id uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    project_oid      uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE RESTRICT,
    full_name        varchar(500) NOT NULL,
    position         varchar(300) NOT NULL,
    phone            varchar(64)  NOT NULL,
    iin              varchar(32)  NOT NULL,
    photo_path       varchar(1024),
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_employees_subcontractor_project
    ON subcontract.employees (subcontractor_id, project_oid);
CREATE INDEX IF NOT EXISTS ix_employees_iin ON subcontract.employees (iin);

CREATE TABLE IF NOT EXISTS subcontract.approval_matrix (
    id                uuid PRIMARY KEY,
    order_no          integer NOT NULL,
    project_oid       uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    subcontractor_id  uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    user_id           uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_matrix_step UNIQUE (project_oid, subcontractor_id, order_no)
);

CREATE TABLE IF NOT EXISTS subcontract.approval_sheet (
    id               uuid PRIMARY KEY,
    employee_id      uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    approver_user_id uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    order_no         integer NOT NULL,
    round_id         uuid NOT NULL,
    status           varchar(32) NOT NULL DEFAULT 'pending',
    decided_at       timestamptz,
    comment          text,
    created_at       timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_approval_sheet_status CHECK (status IN ('pending','approved','rejected','skipped'))
);

CREATE INDEX IF NOT EXISTS ix_approval_sheet_employee_round_order
    ON subcontract.approval_sheet (employee_id, round_id, order_no);
CREATE INDEX IF NOT EXISTS ix_approval_sheet_approver_status
    ON subcontract.approval_sheet (approver_user_id, status);

CREATE TABLE IF NOT EXISTS subcontract.employee_approval_batches (
    id                 uuid PRIMARY KEY,
    subcontractor_id   uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    project_oid        uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE RESTRICT,
    created_by_user_id uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    title              varchar(500) NOT NULL,
    status             varchar(32)  NOT NULL DEFAULT 'draft',
    employee_count     integer NOT NULL DEFAULT 0,
    created_at         timestamptz NOT NULL DEFAULT now(),
    submitted_at       timestamptz,
    CONSTRAINT ck_employee_approval_batches_status CHECK (status IN ('draft','submitted'))
);

CREATE INDEX IF NOT EXISTS ix_employee_approval_batches_subcontractor
    ON subcontract.employee_approval_batches (subcontractor_id, created_at DESC);

CREATE TABLE IF NOT EXISTS subcontract.employee_approval_batch_members (
    batch_id    uuid NOT NULL REFERENCES subcontract.employee_approval_batches(id) ON DELETE CASCADE,
    employee_id uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    added_at    timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (batch_id, employee_id)
);

CREATE INDEX IF NOT EXISTS ix_employee_approval_batch_members_employee
    ON subcontract.employee_approval_batch_members (employee_id);

ALTER TABLE subcontract.employee_approval_batches
    ADD COLUMN IF NOT EXISTS status varchar(32) NOT NULL DEFAULT 'draft';

ALTER TABLE subcontract.employee_approval_batches
    ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();

ALTER TABLE subcontract.approval_sheet
    ADD COLUMN IF NOT EXISTS batch_id uuid REFERENCES subcontract.employee_approval_batches(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_approval_sheet_batch
    ON subcontract.approval_sheet (batch_id);

-- Роль согласующего для сотрудников ТАНСУ
ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS approver_role varchar(32);

CREATE TABLE IF NOT EXISTS subcontract.document_requests (
    id                uuid PRIMARY KEY,
    subcontractor_id  uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    project_oid       uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE RESTRICT,
    created_by_user_id uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    request_type      varchar(32) NOT NULL,
    title             varchar(500) NOT NULL,
    description       text NOT NULL DEFAULT '',
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_document_requests_type CHECK (request_type IN ('leave','ticket','document','expense'))
);

CREATE INDEX IF NOT EXISTS ix_document_requests_subcontractor
    ON subcontract.document_requests (subcontractor_id, created_at DESC);

CREATE TABLE IF NOT EXISTS subcontract.document_approval_matrix (
    id                uuid PRIMARY KEY,
    project_oid       uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    subcontractor_id  uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    request_type      varchar(32) NOT NULL,
    order_no          integer NOT NULL,
    approver_role     varchar(32) NOT NULL,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_document_matrix_step UNIQUE (project_oid, subcontractor_id, request_type, order_no),
    CONSTRAINT ck_document_matrix_type CHECK (request_type IN ('leave','ticket','document','expense')),
    CONSTRAINT ck_document_matrix_role CHECK (approver_role IN ('accounting','hr','finance','management'))
);

CREATE TABLE IF NOT EXISTS subcontract.document_approval_sheet (
    id                   uuid PRIMARY KEY,
    document_request_id  uuid NOT NULL REFERENCES subcontract.document_requests(id) ON DELETE CASCADE,
    approver_user_id     uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    approver_role        varchar(32) NOT NULL,
    order_no             integer NOT NULL,
    round_id             uuid NOT NULL,
    status               varchar(32) NOT NULL DEFAULT 'pending',
    decided_at           timestamptz,
    comment              text,
    created_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_document_approval_sheet_status CHECK (status IN ('pending','approved','rejected','skipped')),
    CONSTRAINT ck_document_approval_sheet_role CHECK (approver_role IN ('accounting','hr','finance','management'))
);

CREATE INDEX IF NOT EXISTS ix_document_approval_sheet_request_round
    ON subcontract.document_approval_sheet (document_request_id, round_id, order_no);
CREATE INDEX IF NOT EXISTS ix_document_approval_sheet_approver_status
    ON subcontract.document_approval_sheet (approver_user_id, status);

CREATE TABLE IF NOT EXISTS subcontract.employee_access_passes (
    id           uuid PRIMARY KEY,
    employee_id  uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    token        varchar(64) NOT NULL,
    issued_at    timestamptz NOT NULL DEFAULT now(),
    revoked_at   timestamptz,
    CONSTRAINT uq_employee_access_passes_token UNIQUE (token)
);

CREATE INDEX IF NOT EXISTS ix_employee_access_passes_employee
    ON subcontract.employee_access_passes (employee_id, issued_at DESC);

CREATE TABLE IF NOT EXISTS subcontract.employee_site_visits (
    id                   uuid PRIMARY KEY,
    employee_id          uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    access_pass_id       uuid REFERENCES subcontract.employee_access_passes(id) ON DELETE SET NULL,
    checked_in_at        timestamptz NOT NULL DEFAULT now(),
    face_confidence      double precision,
    verification_method  varchar(32) NOT NULL DEFAULT 'face_id'
);

CREATE INDEX IF NOT EXISTS ix_employee_site_visits_employee
    ON subcontract.employee_site_visits (employee_id, checked_in_at DESC);

CREATE INDEX IF NOT EXISTS ix_employee_site_visits_checked_in
    ON subcontract.employee_site_visits (checked_in_at DESC);

ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS employee_id uuid
    REFERENCES subcontract.employees(id) ON DELETE CASCADE;

ALTER TABLE subcontract.users DROP CONSTRAINT IF EXISTS ck_users_user_type;
ALTER TABLE subcontract.users ADD CONSTRAINT ck_users_user_type
    CHECK (user_type IN ('TANSU', 'Subcontractor', 'Employee'));

CREATE UNIQUE INDEX IF NOT EXISTS uq_users_employee
    ON subcontract.users (employee_id) WHERE employee_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS subcontract.employee_safety_quiz_completions (
    id               uuid PRIMARY KEY,
    employee_id      uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    score            integer NOT NULL,
    total_questions  integer NOT NULL,
    completed_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_employee_safety_quiz_employee UNIQUE (employee_id)
);

CREATE TABLE IF NOT EXISTS subcontract.employee_ppe_issuances (
    id                uuid PRIMARY KEY,
    employee_id       uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    item_type         varchar(32) NOT NULL,
    size              varchar(32),
    inventory_number  varchar(64),
    issued_at         timestamptz NOT NULL DEFAULT now(),
    issued_by_user_id uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    returned_at       timestamptz,
    notes             varchar(500),
    CONSTRAINT ck_employee_ppe_item_type CHECK (item_type IN ('helmet', 'uniform'))
);

CREATE INDEX IF NOT EXISTS ix_employee_ppe_employee
    ON subcontract.employee_ppe_issuances (employee_id, item_type, issued_at DESC);

CREATE TABLE IF NOT EXISTS subcontract.employee_documents (
    id                   uuid PRIMARY KEY,
    employee_id          uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    name                 varchar(500) NOT NULL,
    document_type        varchar(32) NOT NULL,
    file_path            varchar(1024) NOT NULL,
    uploaded_at          timestamptz NOT NULL DEFAULT now(),
    expires_at           timestamptz,
    uploaded_by_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    CONSTRAINT ck_employee_document_type CHECK (
        document_type IN ('id_card', 'certificate', 'reference', 'medical', 'permit', 'other'))
);

CREATE INDEX IF NOT EXISTS ix_employee_documents_employee
    ON subcontract.employee_documents (employee_id, uploaded_at DESC);

CREATE TABLE IF NOT EXISTS subcontract.employee_block_records (
    id                    uuid PRIMARY KEY,
    employee_id           uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    initiated_by_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    action_type           varchar(16) NOT NULL,
    reason                varchar(1000) NOT NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_employee_block_action_type CHECK (action_type IN ('block', 'unblock'))
);

CREATE INDEX IF NOT EXISTS ix_employee_block_records_employee
    ON subcontract.employee_block_records (employee_id, created_at DESC);

ALTER TABLE subcontract.employees ADD COLUMN IF NOT EXISTS photo_review_status varchar(16);
ALTER TABLE subcontract.employees ADD COLUMN IF NOT EXISTS photo_review_reason varchar(2000);
ALTER TABLE subcontract.employees ADD COLUMN IF NOT EXISTS photo_uploaded_by_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;

CREATE TABLE IF NOT EXISTS subcontract.employee_photo_reviews (
    id                   uuid PRIMARY KEY,
    employee_id          uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    photo_path           varchar(1024) NOT NULL,
    review_type          varchar(16) NOT NULL,
    result               varchar(16) NOT NULL,
    reason               varchar(2000),
    details_json         text,
    reviewed_by_user_id  uuid REFERENCES subcontract.users(id) ON DELETE SET NULL,
    created_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_employee_photo_review_type CHECK (review_type IN ('auto', 'manual')),
    CONSTRAINT ck_employee_photo_review_result CHECK (result IN ('passed', 'failed', 'pending'))
);

CREATE INDEX IF NOT EXISTS ix_employee_photo_reviews_employee
    ON subcontract.employee_photo_reviews (employee_id, created_at DESC);

ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS is_superuser boolean NOT NULL DEFAULT false;

ALTER TABLE subcontract.employee_block_records ADD COLUMN IF NOT EXISTS initiator_role varchar(32);

ALTER TABLE subcontract.employee_documents ADD COLUMN IF NOT EXISTS supersedes_document_id uuid
    REFERENCES subcontract.employee_documents(id) ON DELETE SET NULL;
ALTER TABLE subcontract.employee_documents ADD COLUMN IF NOT EXISTS content_type varchar(64);
ALTER TABLE subcontract.employee_documents ADD COLUMN IF NOT EXISTS expiry_notified_at timestamptz;

UPDATE subcontract.employee_documents SET document_type = 'safety_briefing' WHERE document_type = 'reference';

ALTER TABLE subcontract.employee_documents DROP CONSTRAINT IF EXISTS ck_employee_document_type;
ALTER TABLE subcontract.employee_documents ADD CONSTRAINT ck_employee_document_type CHECK (
    document_type IN ('id_card', 'certificate', 'safety_briefing', 'medical', 'permit', 'other'));

ALTER TABLE subcontract.subcontractors ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE subcontract.subcontractors ADD COLUMN IF NOT EXISTS registered_by_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;

ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS tansu_role varchar(32);
ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS manager_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;

ALTER TABLE subcontract.employee_block_records ADD COLUMN IF NOT EXISTS status varchar(16) NOT NULL DEFAULT 'applied';

CREATE TABLE IF NOT EXISTS subcontract.user_project_assignments (
    user_id     uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE CASCADE,
    project_oid uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    PRIMARY KEY (user_id, project_oid)
);

CREATE TABLE IF NOT EXISTS subcontract.user_subcontractor_assignments (
    user_id          uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE CASCADE,
    subcontractor_id uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, subcontractor_id)
);

ALTER TABLE subcontract.employee_site_visits ADD COLUMN IF NOT EXISTS checked_out_at timestamptz;
ALTER TABLE subcontract.employee_site_visits ADD COLUMN IF NOT EXISTS terminal_location varchar(500);
ALTER TABLE subcontract.employee_site_visits ADD COLUMN IF NOT EXISTS data_source varchar(32) NOT NULL DEFAULT 'face_id';

ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS customer_name varchar(500);
ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS customer_phone varchar(64);
ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS customer_email varchar(256);
ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS budget_amount numeric(18, 2);
ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS budget_currency varchar(8) NOT NULL DEFAULT 'KZT';
ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS responsible_admin_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;
ALTER TABLE subcontract.project_refs ADD COLUMN IF NOT EXISTS project_manager_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;

CREATE TABLE IF NOT EXISTS subcontract.project_documents (
    id                   uuid PRIMARY KEY,
    project_oid          uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    name                 varchar(500) NOT NULL,
    document_type        varchar(32) NOT NULL,
    file_path            varchar(1024) NOT NULL,
    content_type         varchar(128),
    uploaded_at          timestamptz NOT NULL DEFAULT now(),
    uploaded_by_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_project_documents_project
    ON subcontract.project_documents (project_oid, uploaded_at DESC);

ALTER TABLE subcontract.project_subcontractors ADD COLUMN IF NOT EXISTS activity_type varchar(500) NOT NULL DEFAULT '';
ALTER TABLE subcontract.project_subcontractors ADD COLUMN IF NOT EXISTS completion_percent integer NOT NULL DEFAULT 0;
ALTER TABLE subcontract.project_subcontractors ADD COLUMN IF NOT EXISTS progress_reported_at timestamptz;
ALTER TABLE subcontract.project_subcontractors ADD COLUMN IF NOT EXISTS progress_reported_by_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;

ALTER TABLE subcontract.project_subcontractors DROP CONSTRAINT IF EXISTS ck_project_subcontractors_completion_percent;
ALTER TABLE subcontract.project_subcontractors ADD CONSTRAINT ck_project_subcontractors_completion_percent
    CHECK (completion_percent >= 0 AND completion_percent <= 100);

CREATE TABLE IF NOT EXISTS subcontract.user_block_records (
    id                   uuid PRIMARY KEY,
    user_id              uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE CASCADE,
    initiated_by_user_id uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    action_type          varchar(16) NOT NULL,
    reason               varchar(1000) NOT NULL,
    created_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_user_block_action_type CHECK (action_type IN ('block', 'unblock'))
);

CREATE INDEX IF NOT EXISTS ix_user_block_records_user
    ON subcontract.user_block_records (user_id, created_at DESC);

ALTER TABLE subcontract.subcontractors ADD COLUMN IF NOT EXISTS manager_user_id uuid
    REFERENCES subcontract.users(id) ON DELETE SET NULL;

UPDATE subcontract.subcontractors
SET manager_user_id = registered_by_user_id
WHERE manager_user_id IS NULL AND registered_by_user_id IS NOT NULL;

ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS employer_company varchar(64);

CREATE TABLE IF NOT EXISTS subcontract.subcontractor_documents (
    id                   uuid PRIMARY KEY,
    subcontractor_id   uuid NOT NULL REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    name                 varchar(500) NOT NULL,
    document_type        varchar(32) NOT NULL,
    file_path            varchar(1024) NOT NULL,
    content_type         varchar(128),
    uploaded_at          timestamptz NOT NULL DEFAULT now(),
    uploaded_by_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    CONSTRAINT ck_subcontractor_document_type CHECK (
        document_type IN ('contract', 'license', 'insurance', 'charter', 'other'))
);

CREATE INDEX IF NOT EXISTS ix_subcontractor_documents_sub
    ON subcontract.subcontractor_documents (subcontractor_id, uploaded_at DESC);

-- Audit log
CREATE TABLE IF NOT EXISTS subcontract.audit_events (
    id               uuid PRIMARY KEY,
    occurred_at      timestamptz NOT NULL DEFAULT now(),
    actor_user_id    uuid,
    actor_email      varchar(256),
    actor_type       varchar(32) NOT NULL DEFAULT 'system',
    action           varchar(64) NOT NULL,
    entity_type      varchar(64) NOT NULL,
    entity_id        uuid NOT NULL,
    project_oid      uuid,
    subcontractor_id uuid,
    summary          varchar(1000) NOT NULL,
    payload_json     text,
    correlation_id   varchar(128),
    ip_address       varchar(64),
    user_agent       varchar(512)
);
CREATE INDEX IF NOT EXISTS ix_audit_events_occurred ON subcontract.audit_events (occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_events_entity ON subcontract.audit_events (entity_type, entity_id);
CREATE INDEX IF NOT EXISTS ix_audit_events_actor ON subcontract.audit_events (actor_user_id);
CREATE INDEX IF NOT EXISTS ix_audit_events_project ON subcontract.audit_events (project_oid);

-- Delegation, SLA, incidents
CREATE TABLE IF NOT EXISTS subcontract.approver_delegations (
    id                  uuid PRIMARY KEY,
    delegator_user_id   uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    delegate_user_id    uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    project_oid         uuid REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    subcontractor_id    uuid REFERENCES subcontract.subcontractors(id) ON DELETE CASCADE,
    approver_role       varchar(32),
    valid_from          timestamptz NOT NULL,
    valid_to            timestamptz NOT NULL,
    is_active           boolean NOT NULL DEFAULT true,
    created_by_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    created_at          timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_approver_delegations_delegator ON subcontract.approver_delegations (delegator_user_id, is_active);
CREATE INDEX IF NOT EXISTS ix_approver_delegations_delegate ON subcontract.approver_delegations (delegate_user_id, is_active);

CREATE TABLE IF NOT EXISTS subcontract.approval_sla_policies (
    id                      uuid PRIMARY KEY,
    scope                   varchar(32) NOT NULL DEFAULT 'global',
    project_oid             uuid REFERENCES subcontract.project_refs(project_oid) ON DELETE CASCADE,
    request_type            varchar(32),
    pending_days_warning    integer NOT NULL DEFAULT 2,
    pending_days_escalation integer NOT NULL DEFAULT 3,
    escalation_role         varchar(32),
    escalation_user_id      uuid REFERENCES subcontract.users(id) ON DELETE SET NULL,
    is_active               boolean NOT NULL DEFAULT true,
    created_at              timestamptz NOT NULL DEFAULT now()
);

INSERT INTO subcontract.approval_sla_policies (id, scope, pending_days_warning, pending_days_escalation, escalation_role, is_active, created_at)
SELECT 'a0000000-0000-4000-8000-000000000001', 'global', 2, 3, 'management', true, now()
WHERE NOT EXISTS (SELECT 1 FROM subcontract.approval_sla_policies WHERE scope = 'global' AND is_active = true);

ALTER TABLE subcontract.approval_sheet ADD COLUMN IF NOT EXISTS assigned_at timestamptz;
ALTER TABLE subcontract.approval_sheet ADD COLUMN IF NOT EXISTS last_reminder_at timestamptz;
ALTER TABLE subcontract.approval_sheet ADD COLUMN IF NOT EXISTS escalated_at timestamptz;
ALTER TABLE subcontract.approval_sheet ADD COLUMN IF NOT EXISTS acting_for_user_id uuid;

ALTER TABLE subcontract.document_approval_sheet ADD COLUMN IF NOT EXISTS assigned_at timestamptz;
ALTER TABLE subcontract.document_approval_sheet ADD COLUMN IF NOT EXISTS last_reminder_at timestamptz;
ALTER TABLE subcontract.document_approval_sheet ADD COLUMN IF NOT EXISTS escalated_at timestamptz;
ALTER TABLE subcontract.document_approval_sheet ADD COLUMN IF NOT EXISTS acting_for_user_id uuid;

ALTER TABLE subcontract.users ADD COLUMN IF NOT EXISTS notification_email varchar(320);

CREATE TABLE IF NOT EXISTS subcontract.site_incidents (
    id                   uuid PRIMARY KEY,
    project_oid          uuid NOT NULL REFERENCES subcontract.project_refs(project_oid) ON DELETE RESTRICT,
    occurred_at          timestamptz NOT NULL,
    reported_by_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    title                varchar(500) NOT NULL,
    description          varchar(4000) NOT NULL,
    severity             varchar(16) NOT NULL DEFAULT 'medium',
    status               varchar(32) NOT NULL DEFAULT 'open',
    subcontractor_id     uuid REFERENCES subcontract.subcontractors(id) ON DELETE SET NULL,
    block_until_resolved boolean NOT NULL DEFAULT false,
    resolution_notes     varchar(4000),
    resolved_at          timestamptz,
    resolved_by_user_id  uuid REFERENCES subcontract.users(id) ON DELETE SET NULL,
    created_at           timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_site_incidents_project ON subcontract.site_incidents (project_oid);
CREATE INDEX IF NOT EXISTS ix_site_incidents_status ON subcontract.site_incidents (status);

CREATE TABLE IF NOT EXISTS subcontract.site_incident_employees (
    incident_id uuid NOT NULL REFERENCES subcontract.site_incidents(id) ON DELETE CASCADE,
    employee_id uuid NOT NULL REFERENCES subcontract.employees(id) ON DELETE CASCADE,
    PRIMARY KEY (incident_id, employee_id)
);

CREATE TABLE IF NOT EXISTS subcontract.site_incident_comments (
    id              uuid PRIMARY KEY,
    incident_id     uuid NOT NULL REFERENCES subcontract.site_incidents(id) ON DELETE CASCADE,
    author_user_id  uuid NOT NULL REFERENCES subcontract.users(id) ON DELETE RESTRICT,
    body            varchar(4000) NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now()
);
