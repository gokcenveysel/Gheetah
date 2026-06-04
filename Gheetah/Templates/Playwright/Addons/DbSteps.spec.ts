// @ts-nocheck
import { test, expect } from '@playwright/test';

// Database connection example using pg (PostgreSQL)
// Install: npm install pg @types/pg
// Set environment variable: DATABASE_URL=postgresql://user:password@localhost:5432/dbname

test('Database Connection Example', async () => {
  // Given I have a database connection string
  const connectionString = process.env.DATABASE_URL || 'postgresql://localhost:5432/testdb';

  // Skip if no database configured
  test.skip(!process.env.DATABASE_URL, 'DATABASE_URL environment variable not set');

  // When I connect to the database
  const { Pool } = require('pg');
  const pool = new Pool({ connectionString });

  // Then I should be able to query the database
  const result = await pool.query('SELECT 1 as num');
  expect(result.rows[0].num).toBe(1);

  await pool.end();
});
