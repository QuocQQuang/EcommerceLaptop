/**
 * HTML Sanitization Utilities
 * 
 * This module provides secure HTML sanitization using DOMPurify to prevent XSS attacks.
 * All user-generated content should be sanitized before being displayed or stored.
 * 
 * @see FRONTEND_FORM_HANDLING_AND_SECURITY.md
 */

import DOMPurify from 'dompurify';
import React from 'react';

/**
 * Sanitize HTML content for safe display
 * Removes potentially dangerous elements and attributes while preserving basic formatting
 */
export const sanitizeHtml = (dirty: string): string => {
    if (!dirty || typeof dirty !== 'string') {
        return '';
    }

    return DOMPurify.sanitize(dirty, {
        // Use standard HTML profile
        USE_PROFILES: { html: true },

        // Allow only safe formatting tags
        ALLOWED_TAGS: [
            'b', 'i', 'u', 'strong', 'em', 'p', 'br', 'div', 'span',
            'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
            'ul', 'ol', 'li',
            'a'
        ],

        // Allow only safe attributes
        ALLOWED_ATTR: [
            'href', 'title', 'class', 'id'
        ],

        // Forbidden elements that are always dangerous
        FORBID_TAGS: [
            'script', 'object', 'embed', 'form', 'input', 'textarea',
            'button', 'select', 'option', 'style', 'link', 'meta'
        ],

        // Forbidden attributes that can execute JavaScript
        FORBID_ATTR: [
            'onerror', 'onload', 'onclick', 'onmouseover', 'onfocus', 'onblur',
            'onchange', 'onsubmit', 'onreset', 'onselect', 'onkeydown', 'onkeyup',
            'onkeypress', 'onmousedown', 'onmouseup', 'onmousemove', 'onmouseout',
            'style', 'background', 'bgcolor', 'dynsrc', 'lowsrc'
        ],

        // Additional security options
        ALLOW_DATA_ATTR: false,
        ALLOW_UNKNOWN_PROTOCOLS: false,
        SANITIZE_DOM: true,
        KEEP_CONTENT: true
    });
};

/**
 * Sanitize rich text content (for WYSIWYG editors)
 * Allows more HTML tags for rich content while maintaining security
 */
export const sanitizeRichText = (dirty: string): string => {
    if (!dirty || typeof dirty !== 'string') {
        return '';
    }

    return DOMPurify.sanitize(dirty, {
        ALLOWED_TAGS: [
            'b', 'i', 'u', 'strong', 'em', 'p', 'br', 'div', 'span',
            'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
            'ul', 'ol', 'li',
            'a', 'img',
            'blockquote', 'code', 'pre'
        ],

        ALLOWED_ATTR: [
            'href', 'title', 'alt', 'src', 'width', 'height',
            'class', 'id'
        ],

        // Allow only safe protocols for links and images
        ALLOWED_URI_REGEXP: /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|cid|xmpp|data):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i,

        FORBID_TAGS: ['script', 'object', 'embed', 'form', 'input', 'textarea', 'button', 'select', 'option', 'style', 'link', 'meta'],
        FORBID_ATTR: ['onerror', 'onload', 'onclick', 'onmouseover', 'onfocus', 'onblur', 'onchange', 'onsubmit', 'onreset', 'onselect', 'onkeydown', 'onkeyup', 'onkeypress', 'onmousedown', 'onmouseup', 'onmousemove', 'onmouseout', 'style', 'background', 'bgcolor', 'dynsrc', 'lowsrc'],

        ALLOW_DATA_ATTR: false,
        ALLOW_UNKNOWN_PROTOCOLS: false,
        SANITIZE_DOM: true,
        KEEP_CONTENT: true
    });
};

/**
 * Sanitize plain text input
 * Removes HTML tags and normalizes whitespace
 */
export const sanitizeText = (dirty: string): string => {
    if (!dirty || typeof dirty !== 'string') {
        return '';
    }

    // Remove all HTML tags and decode HTML entities
    const textOnly = DOMPurify.sanitize(dirty, {
        ALLOWED_TAGS: [],
        ALLOWED_ATTR: [],
        KEEP_CONTENT: true
    });

    // Normalize whitespace and trim
    return textOnly.replace(/\s+/g, ' ').trim();
};

/**
 * Sanitize URL to prevent javascript: and data: URI attacks
 */
export const sanitizeUrl = (url: string): string => {
    if (!url || typeof url !== 'string') {
        return '';
    }

    // Remove dangerous protocols
    const cleanUrl = url.toLowerCase().trim();

    if (cleanUrl.startsWith('javascript:') ||
        cleanUrl.startsWith('data:') ||
        cleanUrl.startsWith('vbscript:') ||
        cleanUrl.startsWith('file:')) {
        return '';
    }

    // Allow only http, https, mailto, tel protocols
    const allowedProtocols = /^(https?|mailto|tel):/i;
    const protocolMatch = url.match(/^([a-z][a-z0-9+.-]*:)/i);

    if (protocolMatch && !allowedProtocols.test(protocolMatch[0])) {
        return '';
    }

    return DOMPurify.sanitize(url, {
        ALLOWED_TAGS: [],
        ALLOWED_ATTR: [],
        KEEP_CONTENT: true
    });
};

/**
 * Safe component for rendering sanitized HTML
 * Use this instead of dangerouslySetInnerHTML directly
 */
export const SafeHtml: React.FC<{
    html: string;
    className?: string;
    tag?: keyof React.JSX.IntrinsicElements;
    rich?: boolean;
}> = ({ html, className = '', tag: Tag = 'div', rich = false }) => {
    const sanitizedHtml = rich ? sanitizeRichText(html) : sanitizeHtml(html);

    return React.createElement(Tag, {
        className,
        dangerouslySetInnerHTML: { __html: sanitizedHtml }
    });
};

/**
 * Hook for safe content handling
 */
export const useSafeContent = () => {
    const setSafeText = (element: HTMLElement | null, content: string) => {
        if (element) {
            element.textContent = sanitizeText(content);
        }
    };

    const setSafeHtml = (element: HTMLElement | null, content: string) => {
        if (element) {
            element.innerHTML = sanitizeHtml(content);
        }
    };

    return {
        sanitizeHtml,
        sanitizeRichText,
        sanitizeText,
        sanitizeUrl,
        setSafeText,
        setSafeHtml
    };
};