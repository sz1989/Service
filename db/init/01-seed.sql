-- Runs automatically ONLY on first container start, when db_data is empty.
-- Files here execute in alphabetical order after postgres has fully initialized.
-- Flat files to load live in ./db/seed (mounted read-only at /seed).

-- 1. Create the table matching the .NET Person object
CREATE TABLE IF NOT EXISTS person (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    date_of_birth DATE NOT NULL,
    manager_id INTEGER REFERENCES person(id),
    salary NUMERIC(10, 2) NOT NULL
);

-- 2. Load data from the CSV mounted at /seed
COPY person(name, date_of_birth, manager_id, salary)
FROM '/seed/person.csv'
WITH (FORMAT csv, HEADER true);
