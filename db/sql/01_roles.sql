-- Tannous POS Database Roles Setup
-- Run this as superuser (postgres) after database creation

-- Create application roles
CREATE ROLE tannous_app_login WITH LOGIN PASSWORD 'ChangeMe!';
CREATE ROLE tannous_app_rw;
CREATE ROLE tannous_app_ro;

-- Grant read-write role to login role
GRANT tannous_app_rw TO tannous_app_login;

-- Connect to the TannousPOS database
\connect "TannousPOS";

-- Grant database connection rights
GRANT CONNECT ON DATABASE "TannousPOS" TO tannous_app_login;
GRANT USAGE ON SCHEMA public TO tannous_app_rw, tannous_app_ro;

-- Grant table permissions (including future tables)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO tannous_app_rw;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO tannous_app_ro;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO tannous_app_rw;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO tannous_app_ro;

-- Grant sequence permissions (for identity/serial columns)
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO tannous_app_rw;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO tannous_app_rw;

-- Verify roles were created
\du tannous_app*
