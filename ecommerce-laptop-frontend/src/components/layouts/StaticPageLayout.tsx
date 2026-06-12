'use client';

import { AppLink } from '@/components/atoms/AppLink';
import { cn } from '@/lib/utils';
import { useUIStore } from '@/store/uiStore';
import { ChevronRight, Clock, Eye, Share2 } from 'lucide-react';
import React, { useEffect } from 'react';

interface TOCItem {
  id: string;
  title: string;
  level?: number;
}

interface StaticPageLayoutProps {
  title: string;
  subtitle?: string;
  lastUpdated?: string;
  author?: string;
  readTime?: string;
  tableOfContents?: TOCItem[];
  showReadingProgress?: boolean;
  breadcrumbs?: { label: string; href?: string }[];
  children: React.ReactNode;
}

/**
 * Layout component optimized for static content pages
 * Follows design rules for readability, SEO, and trustworthiness
 */
export function StaticPageLayout({
  title,
  subtitle,
  lastUpdated,
  author,
  readTime,
  tableOfContents,
  showReadingProgress = true,
  breadcrumbs,
  children
}: StaticPageLayoutProps) {
  const { readingProgress, setReadingProgress } = useUIStore();

  // Calculate reading progress based on scroll position
  useEffect(() => {
    if (!showReadingProgress) return;

    const calculateProgress = () => {
      const scrolled = window.scrollY;
      const height = document.documentElement.scrollHeight - window.innerHeight;
      const progress = (scrolled / height) * 100;
      setReadingProgress(Math.min(100, Math.max(0, progress)));
    };

    const handleScroll = () => {
      requestAnimationFrame(calculateProgress);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, [showReadingProgress, setReadingProgress]);

  return (
    <div className="min-h-screen bg-white">
      {/* Reading Progress Bar */}
      {showReadingProgress && (
        <div
          className="fixed top-0 left-0 h-1 bg-blue-600 z-50 transition-all duration-150"
          style={{ width: `${readingProgress}%` }}
        />
      )}

      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Breadcrumbs */}
        {breadcrumbs && (
          <nav className="mb-6">
            <ol className="flex items-center space-x-2 text-sm text-gray-600">
              {breadcrumbs.map((crumb, index) => (
                <li key={index} className="flex items-center">
                  {index > 0 && <ChevronRight className="w-4 h-4 mx-2 text-gray-400" />}
                  {crumb.href ? (
                    <AppLink
                      href={crumb.href}
                      pageType="static"
                      className="hover:text-blue-600 transition-colors"
                    >
                      {crumb.label}
                    </AppLink>
                  ) : (
                    <span className="text-gray-900 font-medium">{crumb.label}</span>
                  )}
                </li>
              ))}
            </ol>
          </nav>
        )}

        {/* Header */}
        <header className="mb-8 max-w-4xl">
          <div className="text-center lg:text-left">
            <h1 className="text-4xl lg:text-5xl font-bold text-gray-900 mb-4 leading-tight">
              {title}
            </h1>

            {subtitle && (
              <p className="text-xl text-gray-600 mb-6 leading-relaxed">
                {subtitle}
              </p>
            )}

            {/* Meta Information */}
            <div className="flex flex-wrap items-center justify-center lg:justify-start gap-4 text-sm text-gray-500 mb-6">
              {lastUpdated && (
                <div className="flex items-center">
                  <Clock className="w-4 h-4 mr-1" />
                  <span>Cập nhật: {lastUpdated}</span>
                </div>
              )}

              {author && (
                <div className="flex items-center">
                  <span>Tác giả: {author}</span>
                </div>
              )}

              {readTime && (
                <div className="flex items-center">
                  <Eye className="w-4 h-4 mr-1" />
                  <span>{readTime} phút đọc</span>
                </div>
              )}

              <button
                className="flex items-center hover:text-blue-600 transition-colors"
                onClick={() => navigator.share?.({ title, url: window.location.href })}
              >
                <Share2 className="w-4 h-4 mr-1" />
                <span>Chia s</span>
              </button>
            </div>
          </div>
        </header>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Table of Contents */}
          {tableOfContents && tableOfContents.length > 0 && (
            <aside className="lg:col-span-1">
              <div className="sticky top-8">
                <TableOfContents items={tableOfContents} />
              </div>
            </aside>
          )}

          {/* Main Content */}
          <main className={cn(
            "static-page-content",
            "prose prose-lg max-w-none",
            "prose-headings:text-gray-900 prose-headings:font-semibold",
            "prose-p:text-gray-700 prose-p:leading-7 prose-p:mb-4",
            "prose-a:text-blue-600 prose-a:no-underline hover:prose-a:underline",
            "prose-strong:text-gray-900",
            "prose-ul:my-4 prose-ol:my-4",
            "prose-li:text-gray-700 prose-li:leading-6",
            "prose-blockquote:border-l-4 prose-blockquote:border-blue-200",
            "prose-blockquote:bg-blue-50 prose-blockquote:pl-4 prose-blockquote:py-2",
            "prose-code:bg-gray-100 prose-code:px-1 prose-code:rounded",
            tableOfContents && tableOfContents.length > 0 ? "lg:col-span-3" : "lg:col-span-4"
          )}>
            {children}
          </main>
        </div>

        {/* Trust Indicators Footer */}
        <footer className="mt-12 pt-8 border-t border-gray-200 bg-gray-50 rounded-lg p-6">
          <div className="text-center text-sm text-gray-600">
            <p className="mb-2">
            </p>
            {lastUpdated && (
              <p className="text-xs text-gray-500">
                Lần cập nhật cuối: {lastUpdated}
              </p>
            )}
          </div>
        </footer>
      </div>
    </div>
  );
}

/**
 * Table of Contents Component
 */
function TableOfContents({ items }: { items: TOCItem[] }) {
  const [activeId, setActiveId] = React.useState<string>('');

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setActiveId(entry.target.id);
          }
        });
      },
      { rootMargin: '-20% 0% -80% 0%' }
    );

    items.forEach((item) => {
      const element = document.getElementById(item.id);
      if (element) observer.observe(element);
    });

    return () => observer.disconnect();
  }, [items]);

  return (
    <div className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">
         Mc lc
      </h2>

      <nav className="space-y-2">
        {items.map((item) => (
          <a
            key={item.id}
            href={`#${item.id}`}
            className={cn(
              "block text-sm transition-colors py-1",
              "hover:text-blue-600",
              item.level === 3 && "pl-4 text-gray-600",
              item.level === 4 && "pl-8 text-gray-500",
              activeId === item.id
                ? "text-blue-600 font-medium border-l-2 border-blue-600 pl-3"
                : "text-gray-700"
            )}
            onClick={(e) => {
              e.preventDefault();
              document.getElementById(item.id)?.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
              });
            }}
          >
            {item.title}
          </a>
        ))}
      </nav>
    </div>
  );
}
