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
