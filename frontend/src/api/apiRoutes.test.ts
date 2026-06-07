import { beforeEach, describe, expect, it, vi } from 'vitest';

const { get, post, put, del } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn()
}));

vi.mock('./client', () => ({
  apiClient: {
    get,
    post,
    put,
    delete: del,
    defaults: { baseURL: 'http://test' }
  },
  toApiError: (error: unknown) => error
}));

import { apiRouteRegistry, EXPECTED_API_ROUTE_COUNT } from './apiRouteRegistry';

function resetMocks() {
  get.mockReset().mockResolvedValue({ data: {}, headers: { 'content-disposition': 'filename="journal.pdf"' } });
  post.mockReset().mockResolvedValue({ data: {} });
  put.mockReset().mockResolvedValue({ data: {} });
  del.mockReset().mockResolvedValue({ data: {} });
}

function methodMock(method: 'GET' | 'POST' | 'PUT' | 'DELETE') {
  switch (method) {
    case 'GET':
      return get;
    case 'POST':
      return post;
    case 'PUT':
      return put;
    case 'DELETE':
      return del;
  }
}

describe('api route registry', () => {
  beforeEach(() => {
    resetMocks();
    vi.stubGlobal('URL', {
      createObjectURL: vi.fn(() => 'blob:test'),
      revokeObjectURL: vi.fn()
    });
    vi.stubGlobal('document', {
      createElement: vi.fn(() => ({
        click: vi.fn(),
        href: '',
        download: ''
      }))
    });
  });

  it('contains expected number of routes', () => {
    expect(apiRouteRegistry).toHaveLength(EXPECTED_API_ROUTE_COUNT);
  });

  it('has unique route ids', () => {
    const ids = apiRouteRegistry.map((route) => route.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  describe.each(apiRouteRegistry)('$id', (route) => {
    it(route.kind === 'http' ? `calls ${route.method}` : 'builds url', async () => {
      if (route.kind === 'url') {
        const url = route.invoke();
        expect(url).toMatch(route.pathPattern);
        return;
      }

      await route.invoke();
      const mock = methodMock(route.method);
      expect(mock).toHaveBeenCalledTimes(1);

      const [url] = mock.mock.calls[0] as [string];
      expect(url).toMatch(route.pathPattern);
    });
  });
});
