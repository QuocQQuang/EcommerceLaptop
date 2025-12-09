// Admin authentication error handling utility

/**
 * Handle authentication errors in proxy routes
 * Provides consistent error responses and cookie cleanup
 */
export function handleAuthError(error: Error | Response, cookieName = 'admin-session') {
  // If it's a Response with 401 status
  if (error instanceof Response && error.status === 401) {
    return createAuthErrorResponse('Token expired or invalid', cookieName);
  }

  // If it's a generic authentication error
  if (error instanceof Error && error.message.includes('authentication')) {
    return createAuthErrorResponse(error.message, cookieName);
  }

  // Generic server error
  return createServerErrorResponse();
}

/**
 * Create standardized 401 response with cookie cleanup
 */
export function createAuthErrorResponse(message: string, cookieName: string) {
  const response = new Response(
    JSON.stringify({ 
      success: false, 
      error: message,
      code: 'AUTH_FAILED'
    }),
    { 
      status: 401,
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );

  // Clear expired cookie
  response.headers.append('Set-Cookie', `${cookieName}=; HttpOnly; Secure=${process.env.NODE_ENV === 'production'}; SameSite=Strict; Path=/; Expires=Thu, 01 Jan 1970 00:00:00 GMT`);

  return response;
}

/**
 * Create standardized 500 response
 */
export function createServerErrorResponse() {
  return new Response(
    JSON.stringify({ 
      success: false, 
      error: 'Internal server error',
      code: 'SERVER_ERROR'
    }),
    { 
      status: 500,
      headers: {
        'Content-Type': 'application/json',
      },
    }
  );
}

/**
 * Validate admin token from cookie
 */
export function getAdminTokenFromCookie(request: Request): string | null {
  const cookieHeader = request.headers.get('cookie');
  if (!cookieHeader) return null;

  const cookies = cookieHeader.split(';').reduce((acc, cookie) => {
    const [key, value] = cookie.trim().split('=');
    acc[key] = value;
    return acc;
  }, {} as Record<string, string>);

  return cookies['admin-session'] || null;
}

/**
 * Forward request to backend with proper authentication
 */
export async function forwardToBackend(
  backendPath: string,
  adminToken: string,
  options: {
    method?: string;
    searchParams?: URLSearchParams;
    body?: any;
  } = {}
) {
  const { method = 'GET', searchParams, body } = options;

  // Build backend URL
  const backendUrl = new URL(backendPath, process.env.NEXT_PUBLIC_API_URL);
  if (searchParams) {
    backendUrl.search = searchParams.toString();
  }

  // Prepare request options
  const requestOptions: RequestInit = {
    method,
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${adminToken}`
    },
  };

  if (body && method !== 'GET') {
    requestOptions.body = JSON.stringify(body);
  }

  // Make request to backend
  const backendResponse = await fetch(backendUrl.toString(), requestOptions);

  if (!backendResponse.ok) {
    if (backendResponse.status === 401) {
      throw new Error('Authentication failed');
    }
    throw new Error(`Backend responded with ${backendResponse.status}`);
  }

  return backendResponse.json();
}