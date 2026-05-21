'use client';

import { useState } from 'react';
import { questionsApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Loader2, PlusCircle, X } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';

interface Category {
  id: string;
  name: string;
}

interface SuggestQuestionModalProps {
  categories: Category[];
}

export function SuggestQuestionModal({ categories }: SuggestQuestionModalProps) {
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const [text, setText] = useState('');
  const [options, setOptions] = useState(['', '', '', '']);
  const [correctOptionIndex, setCorrectOptionIndex] = useState(0);
  const [categoryId, setCategoryId] = useState('');
  const [difficulty, setDifficulty] = useState(1);
  const [questionType, setQuestionType] = useState(1); // 1 = Multiple Choice

  const handleOptionChange = (index: number, value: string) => {
    const newOptions = [...options];
    newOptions[index] = value;
    setOptions(newOptions);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    
    if (!categoryId) {
      setError('Please select a category.');
      return;
    }

    if (questionType === 1 && options.some(o => !o.trim())) {
      setError('Please fill in all 4 options.');
      return;
    }

    setLoading(true);
    
    try {
      const payload = {
        text,
        options: questionType === 2 ? ['True', 'False'] : options,
        correctOptionIndex: questionType === 2 ? (correctOptionIndex === 0 ? 0 : 1) : correctOptionIndex,
        categoryId,
        difficulty,
        questionType
      };

      await questionsApi.suggest(payload);
      setSuccess(true);
      
      // Reset form after a delay
      setTimeout(() => {
        setOpen(false);
        setSuccess(false);
        setText('');
        setOptions(['', '', '', '']);
        setCorrectOptionIndex(0);
        setCategoryId('');
      }, 2000);
      
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to submit question.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger 
        render={
          <Button variant="outline" className="w-full font-bold gap-2">
            <PlusCircle className="h-4 w-4" />
            Suggest a Question
          </Button>
        } 
      />
      <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Suggest a New Question</DialogTitle>
          <DialogDescription>
            Help grow SkillDuel by suggesting a new trivia question! It will be reviewed by an admin.
          </DialogDescription>
        </DialogHeader>

        {success ? (
          <div className="py-12 flex flex-col items-center justify-center space-y-4">
            <div className="h-16 w-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center">
              <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h3 className="text-xl font-bold text-center">Question submitted for review!</h3>
            <p className="text-muted-foreground text-center">Thank you for your contribution.</p>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-6 mt-4">
            <div className="space-y-2">
              <Label>Question Text</Label>
              <Input 
                value={text} 
                onChange={(e) => setText(e.target.value)} 
                placeholder="What is the capital of..."
                required
                disabled={loading}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Category</Label>
                <select 
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  required
                  disabled={loading}
                  className="w-full h-10 px-3 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
                >
                  <option value="" disabled>Select Category</option>
                  {categories.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label>Difficulty</Label>
                <select 
                  value={difficulty}
                  onChange={(e) => setDifficulty(Number(e.target.value))}
                  disabled={loading}
                  className="w-full h-10 px-3 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
                >
                  <option value={0}>Easy</option>
                  <option value={1}>Medium</option>
                  <option value={2}>Hard</option>
                </select>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Question Type</Label>
              <div className="flex gap-4">
                <label className="flex items-center gap-2">
                  <input 
                    type="radio" 
                    name="qType" 
                    checked={questionType === 1} 
                    onChange={() => setQuestionType(1)} 
                    disabled={loading}
                  /> 
                  <span className="text-sm">Multiple Choice</span>
                </label>
                <label className="flex items-center gap-2">
                  <input 
                    type="radio" 
                    name="qType" 
                    checked={questionType === 2} 
                    onChange={() => { setQuestionType(2); setCorrectOptionIndex(0); }} 
                    disabled={loading}
                  /> 
                  <span className="text-sm">True / False</span>
                </label>
              </div>
            </div>

            {questionType === 1 ? (
              <div className="space-y-3">
                <Label>Options (Check the correct one)</Label>
                {options.map((opt, i) => (
                  <div key={i} className="flex items-center gap-3">
                    <input 
                      type="radio" 
                      name="correctOption" 
                      checked={correctOptionIndex === i}
                      onChange={() => setCorrectOptionIndex(i)}
                      disabled={loading}
                      className="w-4 h-4"
                    />
                    <Input 
                      value={opt} 
                      onChange={(e) => handleOptionChange(i, e.target.value)} 
                      placeholder={`Option ${i + 1}`}
                      disabled={loading}
                    />
                  </div>
                ))}
              </div>
            ) : (
              <div className="space-y-3">
                 <Label>Correct Answer</Label>
                 <div className="flex gap-4">
                    <label className="flex items-center gap-2">
                      <input 
                        type="radio" 
                        name="correctBool" 
                        checked={correctOptionIndex === 0} 
                        onChange={() => setCorrectOptionIndex(0)} 
                        disabled={loading}
                      /> 
                      <span className="text-sm">True</span>
                    </label>
                    <label className="flex items-center gap-2">
                      <input 
                        type="radio" 
                        name="correctBool" 
                        checked={correctOptionIndex === 1} 
                        onChange={() => setCorrectOptionIndex(1)} 
                        disabled={loading}
                      /> 
                      <span className="text-sm">False</span>
                    </label>
                 </div>
              </div>
            )}

            {error && <p className="text-sm text-destructive">{error}</p>}

            <Button type="submit" className="w-full" disabled={loading}>
              {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Submit Question
            </Button>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
