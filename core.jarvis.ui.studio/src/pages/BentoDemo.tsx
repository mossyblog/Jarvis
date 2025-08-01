/**
 * BentoDemo Page - Demonstrates the Bento Grid System
 * 
 * This page showcases the complete Bento Grid System with
 * interactive components and edit capabilities.
 */

import React from 'react';
import { BentoGridDemo } from '@/components/bento/BentoGridDemo';

const BentoDemo: React.FC = () => {
  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold tracking-tight mb-2">
            Bento Grid System Demo
          </h1>
          <p className="text-muted-foreground max-w-2xl">
            Interactive demonstration of the Bento Grid System - a flexible, 
            responsive layout engine for building dashboard interfaces with 
            drag-and-drop functionality.
          </p>
        </div>
        
        <BentoGridDemo />
      </div>
    </div>
  );
};

export default BentoDemo;