'use client';

import { SecurityUtils } from '@/lib/security-middleware';
import { useCallback, useEffect, useRef, useState } from 'react';

// CSRF Token Hook
export function useCSRFToken(userId?: string) {
  const [csrfToken, setCSRFToken] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const generateToken = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      // In a real implementation, this would be an API call
      const token = await SecurityUtils.generateCSRFToken(userId);
      setCSRFToken(token);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate CSRF token');
    } finally {
      setIsLoading(false);
    }
  }, [userId]);

  useEffect(() => {
    generateToken();
  }, [generateToken]);

  return {
    csrfToken,
    isLoading,
    error,
    regenerateToken: generateToken,
  };
}

// Session Management Hook
export function useSessionSecurity() {
  const [sessionStatus, setSessionStatus] = useState<'loading' | 'valid' | 'invalid' | 'expired'>('loading');
  const [sessionData, setSessionData] = useState<any>(null);
  const [timeUntilExpiry, setTimeUntilExpiry] = useState<number>(0);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const checkSession = useCallback(async () => {
    try {
      // In a real implementation, this would be an API call to validate the session
      const response = await fetch('/api/auth/session', {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Cache-Control': 'no-cache',
        },
      });

      if (response.ok) {
        const data = await response.json();
        setSessionData(data);
        setSessionStatus('valid');

        // Calculate time until expiry
        if (data.expiresAt) {
          const timeLeft = new Date(data.expiresAt).getTime() - Date.now();
          setTimeUntilExpiry(Math.max(0, timeLeft));
        }
      } else if (response.status === 401) {
        setSessionStatus('expired');
        setSessionData(null);
      } else {
        setSessionStatus('invalid');
        setSessionData(null);
      }
    } catch (error) {
      console.error('Session check failed:', error);
      setSessionStatus('invalid');
      setSessionData(null);
    }
  }, []);

  const refreshSession = useCallback(async () => {
    try {
      const response = await fetch('/api/auth/refresh', {
        method: 'POST',
        credentials: 'include',
      });

      if (response.ok) {
        await checkSession();
        return true;
      }
      return false;
    } catch (error) {
      console.error('Session refresh failed:', error);
      return false;
    }
  }, [checkSession]);

  const logout = useCallback(async () => {
    try {
      await fetch('/api/auth/logout', {
        method: 'POST',
        credentials: 'include',
      });
    } catch (error) {
      console.error('Logout failed:', error);
    } finally {
      setSessionStatus('invalid');
      setSessionData(null);
      setTimeUntilExpiry(0);
    }
  }, []);

  useEffect(() => {
    checkSession();

    // Set up periodic session checks
    intervalRef.current = setInterval(() => {
      checkSession();
    }, 5 * 60 * 1000); // Check every 5 minutes

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [checkSession]);

  // Auto-refresh session when close to expiry
  useEffect(() => {
    if (timeUntilExpiry > 0 && timeUntilExpiry < 5 * 60 * 1000) { // 5 minutes before expiry
      refreshSession();
    }
  }, [timeUntilExpiry, refreshSession]);

  return {
    sessionStatus,
    sessionData,
    timeUntilExpiry,
    checkSession,
    refreshSession,
    logout,
    isAuthenticated: sessionStatus === 'valid',
  };
}

// Rate Limiting Hook
export function useRateLimit(endpoint: string, maxRequests: number = 10, windowMs: number = 60000) {
  const [requestCount, setRequestCount] = useState(0);
  const [remainingRequests, setRemainingRequests] = useState(maxRequests);
  const [resetTime, setResetTime] = useState(0);
  const [isRateLimited, setIsRateLimited] = useState(false);

  const makeRequest = useCallback(async (requestFn: () => Promise<any>) => {
    if (isRateLimited) {
      throw new Error('Rate limit exceeded. Please try again later.');
    }

    try {
      const response = await requestFn();

      // Update rate limit info from response headers if available
      if (response.headers) {
        const remaining = response.headers.get('X-RateLimit-Remaining');
        const reset = response.headers.get('X-RateLimit-Reset');

        if (remaining !== null) {
          const remainingCount = parseInt(remaining, 10);
          setRemainingRequests(remainingCount);
          setRequestCount(maxRequests - remainingCount);

          if (remainingCount === 0) {
            setIsRateLimited(true);
            if (reset) {
              setResetTime(new Date(reset).getTime());
            }
          }
        }
      }

      return response;
    } catch (error) {
      if (error instanceof Response && error.status === 429) {
        setIsRateLimited(true);
        const retryAfter = error.headers.get('Retry-After');
        if (retryAfter) {
          setResetTime(Date.now() + parseInt(retryAfter, 10) * 1000);
        }
      }
      throw error;
    }
  }, [isRateLimited, maxRequests]);

  // Reset rate limit when time expires
  useEffect(() => {
    if (isRateLimited && resetTime > 0) {
      const timeUntilReset = resetTime - Date.now();
      if (timeUntilReset <= 0) {
        setIsRateLimited(false);
        setRequestCount(0);
        setRemainingRequests(maxRequests);
        setResetTime(0);
      } else {
        const timeout = setTimeout(() => {
          setIsRateLimited(false);
          setRequestCount(0);
          setRemainingRequests(maxRequests);
          setResetTime(0);
        }, timeUntilReset);

        return () => clearTimeout(timeout);
      }
    }
  }, [isRateLimited, resetTime, maxRequests]);

  return {
    requestCount,
    remainingRequests,
    isRateLimited,
    resetTime,
    makeRequest,
    timeUntilReset: Math.max(0, resetTime - Date.now()),
  };
}

// Security Headers Hook
export function useSecurityHeaders() {
  const [cspViolations, setCspViolations] = useState<any[]>([]);
  const [securityMetrics, setSecurityMetrics] = useState({
    cspViolations: 0,
    xssAttempts: 0,
    mixedContentWarnings: 0,
  });

  useEffect(() => {
    // CSP Violation Reporting
    const handleCSPViolation = (event: SecurityPolicyViolationEvent) => {
      const violation = {
        blockedURI: event.blockedURI,
        documentURI: event.documentURI,
        effectiveDirective: event.effectiveDirective,
        originalPolicy: event.originalPolicy,
        referrer: event.referrer,
        statusCode: event.statusCode,
        violatedDirective: event.violatedDirective,
        timestamp: new Date().toISOString(),
      };

      setCspViolations(prev => [...prev.slice(-9), violation]); // Keep last 10
      setSecurityMetrics(prev => ({
        ...prev,
        cspViolations: prev.cspViolations + 1,
      }));

      // Report to security monitoring service
      if (process.env.NODE_ENV === 'production') {
        fetch('/api/security/csp-report', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(violation),
        }).catch(console.error);
      }
    };

    document.addEventListener('securitypolicyviolation', handleCSPViolation);

    return () => {
      document.removeEventListener('securitypolicyviolation', handleCSPViolation);
    };
  }, []);

  const reportSecurityIncident = useCallback((type: string, details: any) => {
    const incident = {
      type,
      details,
      userAgent: navigator.userAgent,
      url: window.location.href,
      timestamp: new Date().toISOString(),
    };

    if (process.env.NODE_ENV === 'production') {
      fetch('/api/security/incident-report', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(incident),
      }).catch(console.error);
    }

    console.warn('Security incident reported:', incident);
  }, []);

  return {
    cspViolations,
    securityMetrics,
    reportSecurityIncident,
  };
}

// Form Security Hook
export function useFormSecurity() {
  const { csrfToken } = useCSRFToken();
  const [isSecureContext, setIsSecureContext] = useState(false);
  const [formIntegrity, setFormIntegrity] = useState<Map<string, string>>(new Map());

  useEffect(() => {
    // Check if we're in a secure context (HTTPS)
    setIsSecureContext(window.isSecureContext);
  }, []);

  const generateFormIntegrity = useCallback((formData: any): string => {
    const dataString = JSON.stringify(formData, Object.keys(formData).sort());
    return btoa(dataString); // Simple integrity check
  }, []);

  const validateFormIntegrity = useCallback((formId: string, formData: any): boolean => {
    const storedIntegrity = formIntegrity.get(formId);
    if (!storedIntegrity) return true; // No previous data to compare

    const currentIntegrity = generateFormIntegrity(formData);
    return storedIntegrity === currentIntegrity;
  }, [formIntegrity, generateFormIntegrity]);

  const setFormIntegrityHash = useCallback((formId: string, formData: any) => {
    const integrity = generateFormIntegrity(formData);
    setFormIntegrity(prev => new Map(prev).set(formId, integrity));
  }, [generateFormIntegrity]);

  const createSecureFormData = useCallback((data: any) => {
    return {
      ...data,
      _token: csrfToken,
      _timestamp: Date.now(),
      _integrity: generateFormIntegrity(data),
    };
  }, [csrfToken, generateFormIntegrity]);

  return {
    csrfToken,
    isSecureContext,
    createSecureFormData,
    generateFormIntegrity,
    validateFormIntegrity,
    setFormIntegrityHash,
  };
}

// Security Monitoring Hook
export function useSecurityMonitoring() {
  const [securityAlerts, setSecurityAlerts] = useState<any[]>([]);
  const [isMonitoring, setIsMonitoring] = useState(false);

  const startMonitoring = useCallback(() => {
    setIsMonitoring(true);

    // Monitor for suspicious activities
    const suspiciousPatterns = [
      /javascript:/i,
      /<script/i,
      /on\w+\s*=/i,
      /expression\s*\(/i,
    ];

    const checkForSuspiciousContent = (content: string) => {
      for (const pattern of suspiciousPatterns) {
        if (pattern.test(content)) {
          const alert = {
            type: 'xss_attempt',
            pattern: pattern.source,
            content: content.substring(0, 100),
            timestamp: new Date().toISOString(),
            url: window.location.href,
          };

          setSecurityAlerts(prev => [...prev.slice(-19), alert]); // Keep last 20

          // Report to security service
          if (process.env.NODE_ENV === 'production') {
            fetch('/api/security/alert', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(alert),
            }).catch(console.error);
          }

          return true;
        }
      }
      return false;
    };

    // Monitor input fields
    const monitorInputs = () => {
      const inputs = document.querySelectorAll('input, textarea');
      inputs.forEach(input => {
        const handleInput = (event: Event) => {
          const target = event.target as HTMLInputElement;
          if (target.value && checkForSuspiciousContent(target.value)) {
            console.warn('Suspicious content detected in input:', target.name || target.id);
          }
        };

        input.addEventListener('input', handleInput);
      });
    };

    // Monitor for developer tools
    let devToolsOpen = false;
    const detectDevTools = () => {
      const threshold = 160;
      if (window.outerHeight - window.innerHeight > threshold ||
        window.outerWidth - window.innerWidth > threshold) {
        if (!devToolsOpen) {
          devToolsOpen = true;
          const alert = {
            type: 'dev_tools_detected',
            timestamp: new Date().toISOString(),
            url: window.location.href,
          };
          setSecurityAlerts(prev => [...prev.slice(-19), alert]);
        }
      } else {
        devToolsOpen = false;
      }
    };

    monitorInputs();
    const devToolsInterval = setInterval(detectDevTools, 1000);

    return () => {
      clearInterval(devToolsInterval);
    };
  }, []);

  const stopMonitoring = useCallback(() => {
    setIsMonitoring(false);
  }, []);

  useEffect(() => {
    const cleanup = startMonitoring();
    return cleanup;
  }, [startMonitoring]);

  return {
    securityAlerts,
    isMonitoring,
    startMonitoring,
    stopMonitoring,
  };
}

export default {
  useCSRFToken,
  useSessionSecurity,
  useRateLimit,
  useSecurityHeaders,
  useFormSecurity,
  useSecurityMonitoring,
};