// @ts-nocheck
import { test, expect } from '@playwright/test';

test('API GET Request Example', async ({ request }) => {
  // Given I have an API endpoint
  const url = 'https://jsonplaceholder.typicode.com/posts/1';

  // When I make a GET request
  const response = await request.get(url);

  // Then I should receive a 200 status
  expect(response.status()).toBe(200);

  // And the response body should contain an id
  const body = await response.json();
  expect(body).toHaveProperty('id');
});

test('API POST Request Example', async ({ request }) => {
  // Given I have a POST endpoint
  const url = 'https://jsonplaceholder.typicode.com/posts';

  // When I send a POST request
  const response = await request.post(url, {
    data: {
      title: 'Test Post',
      body: 'Test body',
      userId: 1
    }
  });

  // Then I should receive a 201 status
  expect(response.status()).toBe(201);
});
