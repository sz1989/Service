-- Runs automatically ONLY on first container start, when db_data is empty.
-- Files here execute in alphabetical order after postgres has fully initialized.
-- Flat files to load live in ./db/seed (mounted read-only at /seed).

-- Create the table matching the .NET Person object
CREATE TABLE IF NOT EXISTS person (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    date_of_birth DATE NOT NULL,
    manager_id INTEGER REFERENCES person(id),
    salary NUMERIC(10, 2) NOT NULL
);

-- Load data from the CSV mounted at /seed
COPY person(name, date_of_birth, manager_id, salary)
FROM '/seed/person.csv'
WITH (FORMAT csv, HEADER true);

-- Create the inventory table with a JSONB column
CREATE TABLE inventory (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    attributes JSONB NOT NULL
);

-- Because it is a JSON column, rows do not need to share the same schema. We can insert a laptop with a complex nested object, and a backpack with simple flat attributes:
INSERT INTO inventory (name, attributes) VALUES
(
    'Pro Laptop 15"', 
    '{
        "brand": "Dell",
        "category": "Electronics",
        "specs": { "ram_gb": 16, "storage_gb": 512, "cpu": "i7" },
        "tags": ["work", "gaming", "premium"],
        "in_stock": true
     }'
),
(
    'Travel Backpack', 
    '{
        "brand": "Osprey",
        "category": "Luggage",
        "specs": { "capacity_liters": 40 },
        "tags": ["travel", "hiking"],
        "in_stock": false
     }'
);

-- Extracting Text Values & Filtering
-- SELECT name, 
--    attributes->>'brand' AS brand,
--    attributes->'specs'->>'ram_gb' AS ram -- Nested extraction
-- FROM inventory WHERE attributes->>'category' = 'Electronics';

