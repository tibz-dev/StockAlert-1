export interface SessionInfo {
  roles: string[];
  name: string | null;
  email: string | null;
  exp: number | null;
}

export function getSessionInfo(): SessionInfo {
  if (typeof window === 'undefined') {
    return {
      roles: [],
      name: null,
      email: null,
      exp: null,
    };
  }

  const token = window.localStorage.getItem('token');

  if (!token) {
    return {
      roles: [],
      name: null,
      email: null,
      exp: null,
    };
  }

  try {
    const payload = token.split('.')[1];
    const normalized = payload
      .replace(/-/g, '+')
      .replace(/_/g, '/');
    const decoded = JSON.parse(window.atob(normalized));

    const roleValue =
      decoded.role ??
      decoded.roles ??
      decoded[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
      ];

    const roles = Array.isArray(roleValue)
      ? roleValue
      : roleValue
        ? [roleValue]
        : [];

    return {
      roles,
      name:
        decoded.unique_name ??
        decoded.name ??
        decoded[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
        ] ??
        null,
      email:
        decoded.email ??
        decoded[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
        ] ??
        null,
      exp: decoded.exp ? decoded.exp * 1000 : null,
    };
  } catch {
    return {
      roles: [],
      name: null,
      email: null,
      exp: null,
    };
  }
}

export function hasAnyRole(...requiredRoles: string[]) {
  const roles = getSessionInfo().roles;

  return requiredRoles.some((role) => roles.includes(role));
}
